using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Cocoon.ORM
{
    public partial class CocoonORM
    {

        private class CacheItem
        {
            public DateTime Date { get; set; }
            public IEnumerable<object> List { get; set; }
        }

        private Dictionary<string, CacheItem> listCache = new Dictionary<string, CacheItem>();

        /// <summary>
        /// Returns a list of objects
        /// </summary>
        /// <typeparam name="T">Table model</typeparam>
        /// <param name="where">Where expression to use for the query</param>
        /// <param name="top">Maximum number of rows to return</param>
        /// <param name="customParams">Custom parameter object to use with custom columns</param>
        /// <param name="distinct">Select only distinct rows</param>
        /// <param name="timeout">Timeout in milliseconds of query</param>
        /// <param name="cacheSettings"></param>
        /// <param name="fieldsToSelect"></param>
        /// <returns>A list of type T with the result</returns>
        public IEnumerable<T> GetList<T>(
            Expression<Func<T, bool>> where = null,
            int top = 0,
            object customParams = null,
            bool distinct = false,
            int timeout = -1,
            ListCacheSettings cacheSettings = null,
            params Expression<Func<T, object>>[] fieldsToSelect)
        {

            return GetList<T, T>(where, top, customParams, distinct, timeout, cacheSettings, fieldsToSelect);

        }

        /// <summary>
        /// Returns a list of objects
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="where"></param>
        /// <param name="top"></param>
        /// <param name="customParams"></param>
        /// <param name="distinct"></param>
        /// <param name="timeout"></param>
        /// <param name="cacheSettings"></param>
        /// <param name="fieldsToSelect"></param>
        /// <returns></returns>
        public IEnumerable<T> GetListWhereString<T>(
            string where = null,
            int top = 0,
            object customParams = null,
            bool distinct = false,
            int timeout = -1,
            ListCacheSettings cacheSettings = null,
            params Expression<Func<T, object>>[] fieldsToSelect)
        {

            return GetList<T, T>(where, top, customParams, distinct, timeout, cacheSettings, fieldsToSelect);

        }

        /// <summary>
        /// Returns a list of objects
        /// </summary>
        /// <typeparam name="ModelT">Table model to query and use in the where clause</typeparam>
        /// <typeparam name="SubModelT">Table model to return</typeparam>
        /// <param name="where">Where expression to use for the query</param>
        /// <param name="top">Maximum number of rows to return</param>
        /// <param name="customParams">Custom parameter object to use with custom columns</param>
        /// <param name="distinct">Select only distinct rows</param>
        /// <param name="timeout">Timeout in milliseconds of query</param>
        /// <param name="cacheSettings"></param>
        /// <param name="fieldsToSelect"></param>
        /// <returns>List of objects with the result</returns>
        public IEnumerable<SubModelT> GetList<ModelT, SubModelT>(
            Expression<Func<ModelT, bool>> where = null,
            int top = 0,
            object customParams = null,
            bool distinct = false,
            int timeout = -1,
            ListCacheSettings cacheSettings = null,
            params Expression<Func<ModelT, object>>[] fieldsToSelect)
        {

            return GetList<ModelT, SubModelT>(where, top, customParams, distinct, timeout, cacheSettings, fieldsToSelect);

        }

        private IEnumerable<SubModelT> GetList<ModelT, SubModelT>(
            object where = null,
            int top = 0,
            object customParams = null,
            bool distinct = false,
            int timeout = -1,
            ListCacheSettings cacheSettings = null,
            params Expression<Func<ModelT, object>>[] fieldsToSelect)
        {

            if (cacheSettings != null && listCache.ContainsKey(cacheSettings.ID) && DateTime.Now.Subtract(listCache[cacheSettings.ID].Date) < cacheSettings.Timeout)
                return listCache[cacheSettings.ID].List.Cast<SubModelT>();

            TableDefinition def = GetTable(typeof(ModelT));
            List<object> list = new List<object>();

            using (DbConnection conn = Platform.getConnection())
            using (DbCommand cmd = Platform.getCommand(conn, timeout))
            {

                var columns = def.columns;

                if (fieldsToSelect.Length > 0)
                    columns = def.columns.Where(c => fieldsToSelect.Any(f => GetExpressionPropName(f) == c.Name)).ToList();

                Platform.select(conn, cmd, def.objectName, columns, def.joins, def.customColumns, customParams, top, distinct, where);
                Platform.readList(cmd, typeof(SubModelT), list, def.joins);

            }

            if (cacheSettings != null)
                listCache[cacheSettings.ID] = new CacheItem() { Date = DateTime.Now, List = list };

            return list.Cast<SubModelT>();

        }

        /// <summary>
        /// Returns list of objects where values exist in another table
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

            TableDefinition modelDef = GetTable(model);
            TableDefinition inModelDef = GetTable(inModel);

            List<object> list = new List<object>();

            using (DbConnection conn = Platform.getConnection())
            using (DbCommand cmd = Platform.getCommand(conn, timeout))
            {

                //get columns to select
                List<string> columnsToSelect = modelDef.columns.Where(c => !ORMUtilities.HasAttribute<IgnoreOnSelect>(c)).Select(c => $"{modelDef.objectName}.{Platform.getObjectName(c)}").ToList();
                if (columnsToSelect.Count == 0)
                    throw new Exception("No columns to select");

                //generate join clause
                string joinClause = Platform.generateJoinClause(modelDef.objectName, columnsToSelect, modelDef.joins);

                //generate where clauses
                string modelWhereClause = Platform.generateWhereClause(cmd, modelDef.objectName, where, false);
                string inModelWhereClause = Platform.generateWhereClause(cmd, inModelDef.objectName, inWhere);

                //generate top clause
                string topClause = "";
                if (top > 0)
                    topClause = $"top {top}";

                //build sql
                string columns = string.Join(", ", columnsToSelect);
                string modelWhere = modelWhereClause != null ? "and " + modelWhereClause : "";
                cmd.CommandText = $"select {topClause} {string.Join(", ", columnsToSelect)} from {modelDef.objectName} {joinClause} where {modelDef.objectName}.{GetExpressionPropName(modelKey)} in (select {inModelDef.objectName}.{GetExpressionPropName(inModelKey)} from {inModelDef.objectName} {inModelWhereClause}) {modelWhere}";

                //execute sql
                conn.Open();

                Platform.readList(cmd, model, list, modelDef.joins);
            }

            return list.Cast<ModelT>();

        }

        /// <summary>
        /// Returns a list of objects where a key exists in a range of values
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="values"></param>
        /// <param name="where"></param>
        /// <param name="top"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public IEnumerable<T> GetListIn<T>(
            Expression<Func<T, object>> key,
            IEnumerable<object> values,
            Expression<Func<T, bool>> where = null,
            int top = 0,
            int timeout = -1)
        {

            if (values == null || values.Count() == 0)
                return new List<T>();

            Type model = typeof(T);
            TableDefinition modelDef = GetTable(model);
            List<object> list = new List<object>();

            using (DbConnection conn = Platform.getConnection())
            using (DbCommand cmd = Platform.getCommand(conn, timeout))
            {

                //get columns to select
                List<string> columnsToSelect = modelDef.columns.Where(c => !ORMUtilities.HasAttribute<IgnoreOnSelect>(c)).Select(c => $"{modelDef.objectName}.{Platform.getObjectName(c)}").ToList();
                if (columnsToSelect.Count == 0)
                    throw new Exception("No columns to select");

                //generate join clause
                string joinClause = Platform.generateJoinClause(modelDef.objectName, columnsToSelect, modelDef.joins);

                //generate where clauses
                string modelWhereClause = Platform.generateWhereClause(cmd, modelDef.objectName, where, false);

                //generate top clause
                string topClause = "";
                if (top > 0)
                    topClause = $"top {top}";

                //add values
                string valuesPrams = string.Join(",", values.Select(x => Platform.addWhereParam(cmd, x)));

                //build sql
                string columns = string.Join(", ", columnsToSelect);
                string modelWhere = modelWhereClause != null ? "and " + modelWhereClause : "";

                cmd.CommandText = $"select {topClause} {string.Join(", ", columnsToSelect)} from {modelDef.objectName} {joinClause} where {modelDef.objectName}.{GetExpressionPropName(key)} in ({valuesPrams}) {modelWhere}";

                //execute sql
                conn.Open();
                Platform.readList(cmd, model, list, modelDef.joins);

            }

            return list.Cast<T>();

        }

        /// <summary>
        /// Returns a list of objects where a key exists in a range of values
        /// </summary>
        /// <typeparam name="ModelT"></typeparam>
        /// <typeparam name="ValuesT"></typeparam>
        /// <param name="key"></param>
        /// <param name="values"></param>
        /// <param name="where"></param>
        /// <param name="top"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public IEnumerable<ModelT> GetListIn<ModelT, ValuesT>(
            Expression<Func<ModelT, object>> key,
            IEnumerable<ValuesT> values,
            Expression<Func<ModelT, bool>> where = null,
            int top = 0,
            int timeout = -1)
        {

            return GetListIn(key, values.Cast<object>(), where, top, timeout);

        }

        /// <summary>
        /// Returns a single row
        /// </summary>
        /// <typeparam name="T">Table model</typeparam>
        /// <param name="where">Where expression to use for the query</param>
        /// <param name="customParams">Custom parameter object to use with custom columns</param>
        /// <param name="timeout">Timeout in milliseconds of query</param>
        /// <param name="fieldsToSelect"></param>
        /// <returns>An object of type T with the result</returns>
        public T GetSingle<T>(
            Expression<Func<T, bool>> where,
            object customParams = null,
            int timeout = -1,
            params Expression<Func<T, object>>[] fieldsToSelect)
        {

            TableDefinition def = GetTable(typeof(T));

            using (DbConnection conn = Platform.getConnection())
            using (DbCommand cmd = Platform.getCommand(conn, timeout))
            {

                var columns = def.columns;

                if (fieldsToSelect.Length > 0)
                    columns = def.columns.Where(c => fieldsToSelect.Any(f => GetExpressionPropName(f) == c.Name)).ToList();

                Platform.select(conn, cmd, def.objectName, columns, def.joins, def.customColumns, customParams, 1, false, where);
                return Platform.readSingle<T>(cmd, def.joins);
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
        public FieldT GetScalar<ModelT, FieldT>(
            Expression<Func<ModelT, object>> fieldToSelect,
            Expression<Func<ModelT, bool>> where = null,
            int timeout = -1)
        {

            TableDefinition def = GetTable(typeof(ModelT));
            PropertyInfo prop = GetExpressionProp(fieldToSelect);

            using (DbConnection conn = Platform.getConnection())
            using (DbCommand cmd = Platform.getCommand(conn, timeout))
            {
                Platform.select(conn, cmd, def.objectName, new List<PropertyInfo>() { prop }, null, null, null, 1, false, where);
                return Platform.readScalar<FieldT>(cmd);
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
        /// <param name="distinct">Select only distinct rows</param>
        /// <param name="timeout">Timeout in milliseconds of query</param>
        /// <returns>List of values for the selected field</returns>
        public IEnumerable<FieldT> GetScalarList<ModelT, FieldT>(
            Expression<Func<ModelT, object>> fieldToSelect,
            Expression<Func<ModelT, bool>> where = null,
            int top = 0,
            bool distinct = false,
            int timeout = -1)
        {

            TableDefinition def = GetTable(typeof(ModelT));
            PropertyInfo prop = GetExpressionProp(fieldToSelect);

            List<FieldT> list = new List<FieldT>();

            using (DbConnection conn = Platform.getConnection())
            using (DbCommand cmd = Platform.getCommand(conn, timeout))
            {
                Platform.select(conn, cmd, def.objectName, new List<PropertyInfo>() { prop }, null, null, null, top, distinct, where);
                Platform.readScalarList(cmd, list);
            }


            return list;

        }

    }
}
