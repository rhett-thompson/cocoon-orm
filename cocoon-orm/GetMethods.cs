using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Cocoon.ORM
{
    public partial class CocoonORM
    {
        
        /// <summary>
        /// Returns a list of objects
        /// </summary>
        /// <typeparam name="T">Table model</typeparam>
        /// <param name="where">Where expression to use for the query</param>
        /// <param name="top">Maximum number of rows to return</param>
        /// <param name="customParams">Custom parameter object to use with custom columns</param>
        /// <param name="timeout">Timeout in milliseconds of query</param>
        /// <returns>A list of type T with the result</returns>
        public IEnumerable<T> GetList<T>(Expression<Func<T, bool>> where = null, int top = 0, object customParams = null, int timeout = -1)
        {

            return GetList(typeof(T), where, top, customParams, timeout).Cast<T>();

        }

        /// <summary>
        /// Returns a list of objects
        /// </summary>
        /// <typeparam name="T">Table model to return and to use in the where clause</typeparam>
        /// <param name="model">Table model type</param>
        /// <param name="where">Where expression to use for the query</param>
        /// <param name="top">Maximum number of rows to return</param>
        /// <param name="customParams">Custom parameter object to use with custom columns</param>
        /// <param name="timeout">Timeout in milliseconds of query</param>
        /// <returns>List of objects with the result</returns>
        public IEnumerable<object> GetList<T>(Type model, Expression<Func<T, bool>> where = null, int top = 0, object customParams = null, int timeout = -1)
        {

            TableDefinition def = getTable(model);
            List<object> list = new List<object>();

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandTimeout = timeout < 0 ? CommandTimeout : timeout;
                select(conn, cmd, def.objectName, def.columns, def.joins, def.customColumns, customParams, top, where);
                readList(cmd, model, list, def.joins);
            }

            return list;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="ModelT"></typeparam>
        /// <typeparam name="InModelT"></typeparam>
        /// <param name="modelKey"></param>
        /// <param name="inModelKey"></param>
        /// <param name="inWhere"></param>
        /// <param name="where"></param>
        /// <param name="top"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public IEnumerable<ModelT> GetListIn<ModelT, InModelT>(
            Expression<Func<ModelT, object>> modelKey,
            Expression<Func<ModelT, object>> inModelKey,
            Expression<Func<InModelT, bool>> inWhere = null,
            Expression<Func<ModelT, bool>> where = null,
            int top = 0,
            int timeout = -1)
        {

            Type model = typeof(ModelT);
            Type inModel = typeof(InModelT);

            TableDefinition modelDef = getTable(model);
            TableDefinition inModelDef = getTable(inModel);

            List<object> list = new List<object>();

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandTimeout = timeout < 0 ? CommandTimeout : timeout;

                //get columns to select
                List<string> columnsToSelect = modelDef.columns.Where(c => !Utilities.HasAttribute<IgnoreOnSelect>(c)).Select(c => string.Format("{0}.{1}", modelDef.objectName, getObjectName(c))).ToList();
                if (columnsToSelect.Count == 0)
                    throw new Exception("No columns to select");

                //generate join clause
                string joinClause = generateJoinClause(modelDef.objectName, columnsToSelect, modelDef.joins);

                //generate where clauses
                string modelWhereClause = generateWhereClause(cmd, modelDef.objectName, where, false);
                string inModelWhereClause = generateWhereClause(cmd, inModelDef.objectName, inWhere);

                //generate top clause
                string topClause = "";
                if (top > 0)
                    topClause = string.Format("top {0}", top);

                //build sql
                cmd.CommandText = "select {top} {columns} from {model} {joins} where {model}.{modelKey} in (select {inModel}.{inModelKey} from {inModel} {inModelWhere}) {modelWhere}".Inject(new
                {
                    top = topClause,
                    model = modelDef.objectName,
                    inModel = inModelDef.objectName,
                    columns = string.Join(", ", columnsToSelect),
                    joins = joinClause,
                    modelKey = getObjectName(getExpressionProp(modelKey)),
                    inModelKey = getObjectName(getExpressionProp(inModelKey)),
                    inModelWhere = inModelWhereClause,
                    modelWhere = modelWhereClause != null ? "and " + modelWhereClause : ""
                });

                //execute sql
                conn.Open();

                readList(cmd, model, list, modelDef.joins);
            }

            return list.Cast<ModelT>();

        }

        /// <summary>
        /// Returns a single row
        /// </summary>
        /// <typeparam name="T">Table model</typeparam>
        /// <param name="where">Where expression to use for the query</param>
        /// <param name="customParams">Custom parameter object to use with custom columns</param>
        /// <param name="timeout">Timeout in milliseconds of query</param>
        /// <returns>An object of type T with the result</returns>
        public T GetSingle<T>(Expression<Func<T, bool>> where, object customParams = null, int timeout = -1)
        {

            TableDefinition def = getTable(typeof(T));

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandTimeout = timeout < 0 ? CommandTimeout : timeout;
                select(conn, cmd, def.objectName, def.columns, def.joins, def.customColumns, customParams, 1, where);
                return readSingle<T>(cmd, def.joins);
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="ModelT">Table model to return and to use in the where clause</typeparam>
        /// <typeparam name="FieldT">Type of the field to select in the model</typeparam>
        /// <param name="fieldToSelect">Expression pick the field in the model to select</param>
        /// <param name="where">Where expression to use for the query</param>
        /// <param name="timeout">Timeout in milliseconds of query</param>
        /// <returns>The value of the selected field</returns>
        public FieldT GetScalar<ModelT, FieldT>(Expression<Func<ModelT, object>> fieldToSelect, Expression<Func<ModelT, bool>> where = null, int timeout = -1)
        {

            TableDefinition def = getTable(typeof(ModelT));
            PropertyInfo prop = getExpressionProp(fieldToSelect);

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandTimeout = timeout < 0 ? CommandTimeout : timeout;
                select(conn, cmd, def.objectName, new List<MemberInfo>() { prop }, null, null, null, 1, where);
                return readScalar<FieldT>(cmd);
            }

        }

        /// <summary>
        /// Returns a list of scalars
        /// </summary>
        /// <typeparam name="ModelT">Table model to return and to use in the where clause</typeparam>
        /// <typeparam name="FieldT">Type of the field to select in the model</typeparam>
        /// <param name="fieldToSelect">Expression pick the field in the model to select</param>
        /// <param name="where">Where expression to use for the query</param>
        /// <param name="top">Maximum number of rows to return</param>
        /// <param name="timeout">Timeout in milliseconds of query</param>
        /// <returns>List of values for the selected field</returns>
        public IEnumerable<FieldT> GetScalarList<ModelT, FieldT>(Expression<Func<ModelT, object>> fieldToSelect, Expression<Func<ModelT, bool>> where = null, int top = 0, int timeout = -1)
        {

            TableDefinition def = getTable(typeof(ModelT));
            PropertyInfo prop = getExpressionProp(fieldToSelect);

            List<FieldT> list = new List<FieldT>();

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandTimeout = timeout < 0 ? CommandTimeout : timeout;
                select(conn, cmd, def.objectName, new List<MemberInfo>() { prop }, null, null, null, top, where);
                readScalarList(cmd, list);
            }


            return list;

        }
        
    }
}
