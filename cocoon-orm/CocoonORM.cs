using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Linq.Expressions;
using System.Net.NetworkInformation;
using System.Numerics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using StringInject;

namespace Cocoon.ORM
{

    /// <summary>
    /// Database connection
    /// </summary>
    public class CocoonORM
    {

        /// <summary>
        /// The connection string in use by Cocoon
        /// </summary>
        public string ConnectionString;

        /// <summary>
        /// The default timeout in miliseconds of queries
        /// </summary>
        public int CommandTimeout = 15;

        internal Dictionary<Type, TableDefinition> tables = new Dictionary<Type, TableDefinition>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connectionString">The connection string of the database to connect to</param>
        public CocoonORM(string connectionString)
        {

            ConnectionString = connectionString;

        }

        /// <summary>
        /// Pings the database to determine connectivity
        /// </summary>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public PingReply Ping(int timeout = 5000)
        {

            Ping ping = new Ping();
            string server = ConnectionStringParser.GetServerName(ConnectionString);

            if (server.Contains(","))
                server = server.Substring(0, server.LastIndexOf(","));
            else if (server.Contains(":"))
                server = server.Substring(0, server.LastIndexOf(":"));

            return ping.Send(server, timeout);

        }

        #region Database Methods

        /// <summary>
        /// Returns a list of objects
        /// </summary>
        /// <typeparam name="T">Table model</typeparam>
        /// <param name="where">Where expression to use for the query</param>
        /// <param name="top">Maximum number of rows to return</param>
        /// <param name="timeout">Timeout in milliseconds of query</param>
        /// <returns>A list of type T with the result</returns>
        public IEnumerable<T> GetList<T>(Expression<Func<T, bool>> where = null, int top = 0, int timeout = -1)
        {

            return GetList(typeof(T), where, top, timeout).Cast<T>();

        }

        /// <summary>
        /// Returns a list of objects
        /// </summary>
        /// <typeparam name="T">Table model to return and to use in the where clause</typeparam>
        /// <param name="model">Table model type</param>
        /// <param name="where">Where expression to use for the query</param>
        /// <param name="top">Maximum number of rows to return</param>
        /// <param name="timeout">Timeout in milliseconds of query</param>
        /// <returns>List of objects with the result</returns>
        public IEnumerable<object> GetList<T>(Type model, Expression<Func<T, bool>> where = null, int top = 0, int timeout = -1)
        {

            TableDefinition def = getTable(model);
            List<object> list = new List<object>();

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandTimeout = timeout < 0 ? CommandTimeout : timeout;
                select(conn, cmd, def.objectName, def.columns, def.foreignColumns, top, where);
                readList(cmd, model, list);
            }

            return list;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="ModelT"></typeparam>
        /// <typeparam name="InModelT"></typeparam>
        /// <typeparam name="FieldT"></typeparam>
        /// <param name="modelKey"></param>
        /// <param name="inModelKey"></param>
        /// <param name="inWhere"></param>
        /// <param name="where"></param>
        /// <param name="top"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public IEnumerable<ModelT> GetListIn<ModelT, InModelT, FieldT>(
            Expression<Func<ModelT, FieldT>> modelKey,
            Expression<Func<ModelT, FieldT>> inModelKey,
            Expression<Func<InModelT, bool>> inWhere = null,
            Expression<Func<ModelT, bool>> where = null,
            int top = 0,
            int timeout = -1)
        {

            Type model = typeof(ModelT);
            Type inModel = typeof(InModelT);

            MemberExpression modelKeyExpression = (MemberExpression)modelKey.Body;
            MemberExpression inModelKeyExpression = (MemberExpression)inModelKey.Body;

            TableDefinition modelDef = getTable(model);
            TableDefinition inModelDef = getTable(inModel);

            List<object> list = new List<object>();

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandTimeout = timeout < 0 ? CommandTimeout : timeout;

                //get columns to select
                List<string> columnsToSelect = modelDef.columns.Where(c => !HasAttribute<IgnoreOnSelect>(c)).Select(c => string.Format("{0}.{1}", modelDef.objectName, getObjectName(c))).ToList();
                if (columnsToSelect.Count == 0)
                    throw new Exception("No columns to select");

                //generate join clause
                string joinClause = generateJoinClause(modelDef.objectName, columnsToSelect, modelDef.foreignColumns);

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
                    modelKey = getObjectName(modelKeyExpression.Member),
                    inModelKey = getObjectName(inModelKeyExpression.Member),
                    inModelWhere = inModelWhereClause,
                    modelWhere = modelWhereClause != null ? "and " + modelWhereClause : ""
                });

                //execute sql
                conn.Open();

                readList(cmd, model, list);
            }

            return list.Cast<ModelT>();

        }

        /// <summary>
        /// Returns a single row
        /// </summary>
        /// <typeparam name="T">Table model</typeparam>
        /// <param name="where">Where expression to use for the query</param>
        /// <param name="timeout">Timeout in milliseconds of query</param>
        /// <returns>An object of type T with the result</returns>
        public T GetSingle<T>(Expression<Func<T, bool>> where, int timeout = -1)
        {

            TableDefinition def = getTable(typeof(T));

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandTimeout = timeout < 0 ? CommandTimeout : timeout;
                select(conn, cmd, def.objectName, def.columns, def.foreignColumns, 1, where);
                return readSingle<T>(cmd);
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
        public FieldT GetScalar<ModelT, FieldT>(Expression<Func<ModelT, FieldT>> fieldToSelect, Expression<Func<ModelT, bool>> where = null, int timeout = -1)
        {

            MemberExpression expression = (MemberExpression)fieldToSelect.Body;
            TableDefinition def = getTable(expression.Member.DeclaringType);

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandTimeout = timeout < 0 ? CommandTimeout : timeout;
                select(conn, cmd, def.objectName, new List<MemberInfo>() { expression.Member }, null, 1, where);
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
        public IEnumerable<FieldT> GetScalarList<ModelT, FieldT>(Expression<Func<ModelT, FieldT>> fieldToSelect, Expression<Func<ModelT, bool>> where = null, int top = 0, int timeout = -1)
        {

            MemberExpression expression = (MemberExpression)fieldToSelect.Body;
            TableDefinition def = getTable(expression.Member.DeclaringType);

            List<FieldT> list = new List<FieldT>();

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandTimeout = timeout < 0 ? CommandTimeout : timeout;
                select(conn, cmd, def.objectName, new List<MemberInfo>() { expression.Member }, null, top, where);
                readScalarList(cmd, list);
            }


            return list;

        }

        /// <summary>
        /// Deletes records from a table
        /// </summary>
        /// <typeparam name="T">Table model</typeparam>
        /// <param name="where">Where expression to use for the query</param>
        /// <param name="timeout">Timeout in milliseconds of query</param>
        /// <returns>The number of records that were affected</returns>
        public int Delete<T>(Expression<Func<T, bool>> where, int timeout = -1)
        {

            return Delete(typeof(T), where, timeout);

        }

        /// <summary>
        /// Deletes records from a table
        /// </summary>
        /// <typeparam name="T">Table model</typeparam>
        /// /// <param name="model">Table model type</param>
        /// <param name="where">Where expression to use for the query</param>
        /// <param name="timeout">Timeout in milliseconds of query</param>
        /// <returns>The number of records that were affected</returns>
        public int Delete<T>(Type model, Expression<Func<T, bool>> where, int timeout = -1)
        {
            TableDefinition def = getTable(model);

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandTimeout = timeout < 0 ? CommandTimeout : timeout;
                string whereClause = generateWhereClause(cmd, def.objectName, where);

                cmd.CommandText = "delete from {model} {where}".Inject(new { model = def.objectName, where = whereClause });

                conn.Open();
                return cmd.ExecuteNonQuery();

            }
        }

        /// <summary>
        /// Updates records in a table
        /// </summary>
        /// <typeparam name="T">Table model to use in the where clause</typeparam>
        /// <param name="objectToUpdate">Object to update in the table. The table model is inferred from the Type of this object.</param>
        /// <param name="where">Where expression to use for the query</param>
        /// <param name="timeout">Timeout in milliseconds of query</param>
        /// <returns>The number of records that were affected</returns>
        public int Update<T>(object objectToUpdate, Expression<Func<T, bool>> where = null, int timeout = -1)
        {

            if (objectToUpdate == null)
                throw new NullReferenceException("objectToUpdate cannot be null.");

            TableDefinition def = getTable(objectToUpdate.GetType());

            return update(def, objectToUpdate, def.columns, timeout, where);


        }

        /// <summary>
        /// Updates a subset of fields in a table
        /// </summary>
        /// <typeparam name="T">Table model to use in the where clause</typeparam>
        /// <param name="fieldsToUpdate">An object containg the fields/values to update</param>
        /// <param name="where">Where expression to use for the query</param>
        /// <param name="timeout">Timeout in milliseconds of query</param>
        /// <returns>The number of records that were affected</returns>
        public int UpdatePartial<T>(object fieldsToUpdate, Expression<Func<T, bool>> where = null, int timeout = -1)
        {

            if (fieldsToUpdate == null)
                throw new NullReferenceException("fieldsToUpdate cannot be null.");

            TableDefinition def = getTable(typeof(T));

            return update(def, fieldsToUpdate, fieldsToUpdate.GetType().GetProperties(), timeout, where);

        }

        /// <summary>
        /// Inserts a single row into a table
        /// </summary>
        /// <typeparam name="T">Table model to use in the where clause and return</typeparam>
        /// <param name="objectToInsert">Object to insert into the table The table model is inferred from the Type of this object.</param>
        /// <param name="timeout">Timeout in milliseconds of query</param>
        /// <returns>The newly inserted object of type T</returns>
        public T Insert<T>(object objectToInsert, int timeout = -1)
        {

            return insert<T>(objectToInsert.GetType(), objectToInsert, timeout);

        }

        /// <summary>
        /// Inserts a single object
        /// </summary>
        /// <typeparam name="T">Table model to use in the where clause and return</typeparam>
        /// <param name="objectToInsert">Object to insert into the table The table model is inferred from the Type of this object.</param>
        /// <param name="timeout">Timeout in milliseconds of query</param>
        /// <returns>The newly inserted object of type T</returns>
        public T Insert<T>(T objectToInsert, int timeout = -1)
        {

            return insert<T>(typeof(T), objectToInsert, timeout);

        }

        /// <summary>
        /// Inserts a list of objects
        /// </summary>
        /// <typeparam name="T">Table model to use in the where clause and return</typeparam>
        /// <param name="objectsToInsert">Objects to insert into the database</param>
        /// <param name="timeout">Timeout in milliseconds of query</param>
        /// <returns>The newly inserted objects of type T</returns>
        public IEnumerable<T> InsertList<T>(IEnumerable<T> objectsToInsert, int timeout = -1)
        {

            List<T> list = new List<T>();

            foreach (T obj in objectsToInsert)
                list.Add(Insert(obj, timeout));

            return list;

        }

        /// <summary>
        /// Returns the number of rows that exist for a query
        /// </summary>
        /// <typeparam name="T">Table model</typeparam>
        /// <param name="where">Where expression to use for the query</param>
        /// <param name="timeout">Timeout in milliseconds of query</param>
        /// <returns>The number of rows</returns>
        public int Count<T>(Expression<Func<T, bool>> where = null, int timeout = -1)
        {

            TableDefinition def = getTable(typeof(T));

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = conn.CreateCommand())
            {

                cmd.CommandTimeout = timeout < 0 ? CommandTimeout : timeout;

                //generate where clause
                string whereClause = generateWhereClause(cmd, def.objectName, where);

                //generate sql
                cmd.CommandText = "select count(*) from {model} {where}".Inject(new { model = def.objectName, where = whereClause });

                //execute sql
                conn.Open();
                return readScalar<int>(cmd);

            }

        }

        /// <summary>
        /// Returns a binary checksum on a query
        /// </summary>
        /// <typeparam name="T">Table model</typeparam>
        /// <param name="where">Where expression to use for the query</param>
        /// <param name="timeout">Timeout in milliseconds of query</param>
        /// <returns>An integer checksum hash</returns>
        public int Checksum<T>(Expression<Func<T, bool>> where = null, int timeout = -1)
        {

            TableDefinition def = getTable(typeof(T));

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandTimeout = timeout < 0 ? CommandTimeout : timeout;

                //generate where clause
                string whereClause = generateWhereClause(cmd, def.objectName, where);

                //generate sql
                cmd.CommandText = "select checksum_agg(binary_checksum(*)) from {model} {where}".Inject(new { model = def.objectName, where = whereClause });

                //execute sql
                conn.Open();
                return readScalar<int>(cmd);

            }

        }

        /// <summary>
        /// Determines of rows exist
        /// </summary>
        /// <typeparam name="T">Table model</typeparam>
        /// <param name="where">Where expression to use for the query</param>
        /// <returns>True if rows exists, False otherwise</returns>
        public bool Exists<T>(Expression<Func<T, bool>> where = null)
        {

            return Count(where) > 0;

        }

        /// <summary>
        /// Copies rows into the same table
        /// </summary>
        /// <typeparam name="T">Table model</typeparam>
        /// <param name="where">Where expression to use for the query</param>
        /// <param name="overrideValues"></param>
        /// <param name="timeout">Timeout in milliseconds of query</param>
        /// <returns>The newly inserted rows</returns>
        public IEnumerable<T> Copy<T>(Expression<Func<T, bool>> where, object overrideValues = null, int timeout = -1)
        {
            TableDefinition def = getTable(typeof(T));

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandTimeout = timeout < 0 ? CommandTimeout : timeout;
                string whereClause = generateWhereClause(cmd, def.objectName, where);

                //get columns and values
                List<string> columns = new List<string>();
                List<string> values = new List<string>();
                PropertyInfo[] overrideValueProps = overrideValues != null ? overrideValues.GetType().GetProperties() : new PropertyInfo[0];
                foreach (PropertyInfo prop in def.columns)
                    if (!HasAttribute<IgnoreOnInsert>(prop))
                    {

                        string column = string.Format("{0}.{1}", def.objectName, getObjectName(prop));
                        columns.Add(column);

                        PropertyInfo overrideProp = overrideValueProps.SingleOrDefault(p => p.Name == prop.Name);
                        if (overrideProp != null)
                            values.Add(addParam(cmd, "override_value_" + getGuidString(), overrideProp.GetValue(overrideValues)).ParameterName);
                        else
                            values.Add(column);

                    }

                //get primary keys
                List<string> outputTableKeys = new List<string>();
                List<string> insertedPrimaryKeys = new List<string>();
                List<string> wherePrimaryKeys = new List<string>();
                foreach (PropertyInfo pk in def.primaryKeys)
                {
                    string primaryKeyName = getObjectName(pk);
                    outputTableKeys.Add("{key} {type}".Inject(new { key = primaryKeyName, type = dbTypeMap[pk.PropertyType] }));
                    insertedPrimaryKeys.Add("inserted." + primaryKeyName);
                    wherePrimaryKeys.Add("ids.{primaryKey} = {model}.{primaryKey}".Inject(new { primaryKey = primaryKeyName, model = def.objectName }));
                }

                //build sql
                cmd.CommandText = "{insertedTable};insert into {model} ({columns}) output {insertedPrimaryKeys} into @ids select {values} from {model} {where};select {model}.* from {model} join @ids ids on {wherePrimaryKeys}".Inject(new
                {
                    insertedTable = string.Format("declare @ids table({0})", string.Join(", ", outputTableKeys)),
                    model = def.objectName,
                    columns = string.Join(", ", columns),
                    insertedPrimaryKeys = string.Join(", ", insertedPrimaryKeys),
                    values = string.Join(", ", values),
                    wherePrimaryKeys = string.Join(" and ", wherePrimaryKeys),
                    where = whereClause
                });

                conn.Open();

                List<object> list = new List<object>();
                readList(cmd, def.type, list);

                return list.Cast<T>();

            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="where"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public string MD5HashInDB<T>(Expression<Func<T, bool>> where = null, int top = 0, int timeout = -1)
        {

            TableDefinition def = getTable(typeof(T));

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandTimeout = timeout < 0 ? CommandTimeout : timeout;

                //generate where clause
                string whereClause = generateWhereClause(cmd, def.objectName, where);

                //generate columns
                List<string> columns = new List<string>();
                foreach (MemberInfo member in def.columns)
                    if (((PropertyInfo)member).PropertyType == typeof(DateTime) || ((PropertyInfo)member).PropertyType == typeof(DateTime?))
                        columns.Add("format({column}, 'MM/dd/yyyy H:mm:ss')".InjectSingleValue("column", getObjectName(member)));
                    else
                        columns.Add(getObjectName(member));

                //generate top clause
                string topClause = "";
                if (top > 0)
                    topClause = string.Format("top {0}", top);

                //generate sql
                cmd.CommandText = @"
                    declare @hashes table (md5 varchar(32))
                    declare @list varchar(max)
                    insert into @hashes select {top} convert(varchar(32), hashbytes('MD5', convert(varchar(1000), concat({columns}))), 2) as md5 from {model} {where}
                    select @list = coalesce(@list + ',', '') + md5 from @hashes
                    select convert(varchar(32), hashbytes('MD5', @list), 2)
                ".Inject(new
                {
                    top = topClause,
                    columns = string.Join(", ',', ", columns),
                    model = def.objectName,
                    where = whereClause
                });

                //execute sql
                conn.Open();
                return readScalar<string>(cmd);

            }

        }

        #endregion

        #region SQL

        /// <summary>
        /// Executes a SQL statement for a list of rows
        /// </summary>
        /// <typeparam name="T">Table model</typeparam>
        /// <param name="sql">SQL statement string</param>
        /// <param name="parameters">Object containing the parameters of the query. Members of this object that match @Parameter variables in the SQL will be parameterized.</param>
        /// <param name="timeout">Timeout in milliseconds of query</param>
        /// <returns>A list of type T with the result</returns>
        public IEnumerable<T> ExecuteSQLList<T>(string sql, object parameters = null, int timeout = -1)
        {

            bool isScalar = !HasAttribute<Table>(typeof(T));
            List<object> list = new List<object>();

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandTimeout = timeout < 0 ? CommandTimeout : timeout;
                addParamObject(cmd, parameters);

                cmd.CommandText = sql;
                conn.Open();

                if (isScalar)
                    readScalarList(cmd, list);
                else
                    readList(cmd, typeof(T), list);

            }

            return list.Cast<T>();

        }

        /// <summary>
        /// Executes a SQL statement for a single row
        /// </summary>
        /// <typeparam name="T">Table model</typeparam>
        /// <param name="sql">SQL statement string</param>
        /// <param name="parameters">Object containing the parameters of the query. Members of this object that match @Parameter variables in the SQL will be parameterized.</param>
        /// <param name="timeout">Timeout in milliseconds of query</param>
        /// <returns>An object of type T with the result</returns>
        public T ExecuteSQLSingle<T>(string sql, object parameters = null, int timeout = -1)
        {

            bool isScalar = !HasAttribute<Table>(typeof(T));

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandTimeout = timeout < 0 ? CommandTimeout : timeout;
                addParamObject(cmd, parameters);

                cmd.CommandText = sql;
                conn.Open();

                if (isScalar)
                    return readScalar<T>(cmd);
                else
                    return readSingle<T>(cmd);

            }

        }

        /// <summary>
        /// Executes a SQL statement with no sesult
        /// </summary>
        /// <param name="sql">SQL statement string</param>
        /// <param name="parameters">Object containing the parameters of the query. Members of this object that match @Parameter variables in the SQL will be parameterized.</param>
        /// <param name="timeout">Timeout in milliseconds of query</param>
        /// <returns>The number of records affected by the query</returns>
        public int ExecuteSQLVoid(string sql, object parameters = null, int timeout = -1)
        {

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = conn.CreateCommand())
            {

                cmd.CommandTimeout = timeout < 0 ? CommandTimeout : timeout;
                addParamObject(cmd, parameters);

                cmd.CommandText = sql;
                conn.Open();

                return cmd.ExecuteNonQuery();

            }

        }

        /// <summary>
        /// Executes a SQL statement for DataSet
        /// </summary>
        /// <param name="sql">SQL statement string</param>
        /// <param name="parameters">Object containing the parameters of the query. Members of this object that match @Parameter variables in the SQL will be parameterized.</param>
        /// <param name="timeout">Timeout in milliseconds of query</param>
        /// <returns>A DataSet with the result of the query</returns>
        public DataSet ExecuteSQLDataSet(string sql, object parameters = null, int timeout = -1)
        {

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = conn.CreateCommand())
            {

                cmd.CommandTimeout = timeout < 0 ? CommandTimeout : timeout;

                addParamObject(cmd, parameters);

                cmd.CommandText = sql;

                using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                {
                    DataSet ds = new DataSet();
                    adapter.Fill(ds);
                    return ds;
                }

            }

        }

        #endregion

        #region Stored Procedures

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T">Table model</typeparam>
        /// <param name="procedureName">The name of the procedure to execute</param>
        /// <param name="parameters">An object containing the parameters of the stored procedure</param>
        /// <param name="timeout">Timeout in milliseconds of query</param>
        /// <returns>A list of type T with the result</returns>
        public IEnumerable<T> ExecuteProcList<T>(string procedureName, object parameters = null, int timeout = -1)
        {

            bool isScalar = !HasAttribute<Table>(typeof(T));
            List<object> list = new List<object>();

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandTimeout = timeout < 0 ? CommandTimeout : timeout;
                addParamObject(cmd, parameters);

                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = procedureName;
                conn.Open();

                if (isScalar)
                    readScalarList(cmd, list);
                else
                    readList(cmd, typeof(T), list);

            }

            return list.Cast<T>();

        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T">Table model</typeparam>
        /// <param name="procedureName">The name of the procedure to execute</param>
        /// <param name="parameters">An object containing the parameters of the stored procedure</param>
        /// <param name="timeout">Timeout in milliseconds of query</param>
        /// <returns>An object of type T with the result</returns>
        public T ExecuteProcSingle<T>(string procedureName, object parameters = null, int timeout = -1)
        {

            bool isScalar = !HasAttribute<Table>(typeof(T));

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandTimeout = timeout < 0 ? CommandTimeout : timeout;
                addParamObject(cmd, parameters);

                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = procedureName;
                conn.Open();

                if (isScalar)
                    return readScalar<T>(cmd);
                else
                    return readSingle<T>(cmd);

            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="procedureName">The name of the procedure to execute</param>
        /// <param name="parameters">An object containing the parameters of the stored procedure</param>
        /// <param name="timeout">Timeout in milliseconds of query</param>
        /// <returns>The number of rows affected</returns>
        public int ExecuteProcVoid(string procedureName, object parameters = null, int timeout = -1)
        {

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandTimeout = timeout < 0 ? CommandTimeout : timeout;
                addParamObject(cmd, parameters);

                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = procedureName;
                conn.Open();

                return cmd.ExecuteNonQuery();

            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="procedureName">The name of the procedure to execute</param>
        /// <param name="parameters">An object containing the parameters of the stored procedure</param>
        /// <param name="timeout">Timeout in milliseconds of query</param>
        /// <returns>A DataSet with the result</returns>
        public DataSet ExecuteProcDataSet(string procedureName, object parameters = null, int timeout = -1)
        {

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandTimeout = timeout < 0 ? CommandTimeout : timeout;
                addParamObject(cmd, parameters);

                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = procedureName;

                using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                {
                    DataSet ds = new DataSet();
                    adapter.Fill(ds);
                    return ds;
                }

            }

        }

        #endregion

        #region Utilities

        internal const string base36Digits = "0123456789abcdefghijklmnopqrstuvwxyz";
        internal static DateTime baseDate = new DateTime(1900, 1, 1);

        /// <summary>
        /// Changes the type of an object
        /// </summary>
        /// <param name="value"></param>
        /// <param name="conversionType"></param>
        /// <returns></returns>
        public static object ChangeType(object value, Type conversionType)
        {

            if (value == null || value == DBNull.Value)
                if (conversionType.IsValueType)
                    return Activator.CreateInstance(conversionType);
                else
                    return null;

            if (value.GetType() == conversionType)
                return value;

            if (conversionType.IsGenericType && conversionType.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
                conversionType = Nullable.GetUnderlyingType(conversionType);

            try
            {
                return TypeDescriptor.GetConverter(conversionType).ConvertFrom(value);
            }
            catch
            {
                return Convert.ChangeType(value, conversionType);

            }

        }

        /// <summary>
        /// Generates a sequential COMB GUID
        /// </summary>
        /// <returns></returns>
        public static Guid GenerateSequentialGuid()
        {

            byte[] guidArray = Guid.NewGuid().ToByteArray();

            DateTime now = DateTime.Now;

            TimeSpan days = new TimeSpan(now.Ticks - baseDate.Ticks);
            TimeSpan msecs = now.TimeOfDay;

            byte[] daysArray = BitConverter.GetBytes(days.Days);
            byte[] msecsArray = BitConverter.GetBytes((long)(msecs.TotalMilliseconds / 3.333333));

            Array.Reverse(daysArray);
            Array.Reverse(msecsArray);

            Array.Copy(daysArray, daysArray.Length - 2, guidArray, guidArray.Length - 6, 2);
            Array.Copy(msecsArray, msecsArray.Length - 4, guidArray, guidArray.Length - 4, 4);

            return new Guid(guidArray);

        }

        /// <summary>
        /// Generates a sequential Base36 unique identifier
        /// </summary>
        /// <returns></returns>
        public static string GenerateSequentialUID()
        {

            return Base36Encode(DateTime.Now.Ticks);

        }

        /// <summary>
        /// Decode Base36 string
        /// </summary>
        /// <param name="base36Encoded"></param>
        /// <returns></returns>
        public static long Base36Decode(string base36Encoded)
        {

            if (string.IsNullOrWhiteSpace(base36Encoded))
                throw new ArgumentException("Empty value.");

            base36Encoded = base36Encoded.ToUpper();

            bool negative = false;

            if (base36Encoded[0] == '-')
            {
                negative = true;
                base36Encoded = base36Encoded.Substring(1, base36Encoded.Length - 1);
            }

            if (base36Encoded.Any(c => !base36Digits.Contains(c)))
                throw new ArgumentException("Invalid value: \"" + base36Encoded + "\".");

            long decoded = 0L;

            for (var i = 0; i < base36Encoded.Length; ++i)
                decoded += base36Digits.IndexOf(base36Encoded[i]) * (long)BigInteger.Pow(base36Digits.Length, base36Encoded.Length - i - 1);

            return negative ? decoded * -1 : decoded;

        }

        /// <summary>
        /// Base36 encode a value
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string Base36Encode(long value)
        {
            if (value == long.MinValue)
                return "-1Y2P0IJ32E8E8";

            bool negative = value < 0;

            value = Math.Abs(value);

            string encoded = string.Empty;

            do
                encoded = base36Digits[(int)(value % base36Digits.Length)] + encoded;

            while ((value /= base36Digits.Length) != 0);

            return negative ? "-" + encoded : encoded;

        }

        /// <summary>
        /// Decode Base64 string
        /// </summary>
        /// <param name="base64Encoded"></param>
        /// <returns></returns>
        public static string Base64Decode(string base64Encoded)
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(base64Encoded));
        }

        /// <summary>
        /// Base64 encode a string
        /// </summary>
        /// <param name="plainText"></param>
        /// <returns></returns>
        public static string Base64Encode(string plainText)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(plainText));
        }

        /// <summary>
        /// Determines of the member has a custom attribute
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="member"></param>
        /// <returns></returns>
        public static bool HasAttribute<T>(MemberInfo member)
        {

            return member.GetCustomAttributes(typeof(T), false).Length > 0;

        }

        /// <summary>
        /// Determines of a class has a custom attribute
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="property"></param>
        /// <returns></returns>
        public static bool HasAttribute<T>(Type property)
        {

            return property.GetCustomAttributes(typeof(T), false).Length > 0;

        }

        /// <summary>
        /// Creates a list of scalars from a single field from a list of rows
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="rows"></param>
        /// <param name="fieldToMap"></param>
        /// <returns></returns>
        public static IEnumerable<T> FillScalarList<T>(IEnumerable<DataRow> rows, string fieldToMap = null)
        {

            List<T> list = new List<T>();
            foreach (DataRow row in rows)
            {
                if (fieldToMap == null)
                    list.Add((T)ChangeType(row[0], typeof(T)));
                else if (row.Table.Columns.Contains(fieldToMap))
                    list.Add((T)ChangeType(row[fieldToMap], typeof(T)));
            }
            return list;

        }

        /// <summary>
        /// Creates a list of scalars from a single field from a DataTable
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="table"></param>
        /// <param name="fieldToMap"></param>
        /// <returns></returns>
        public static IEnumerable<T> FillScalarList<T>(DataTable table, string fieldToMap = null)
        {

            return FillScalarList<T>(table.Select(), fieldToMap);

        }

        /// <summary>
        /// Fills a list from a list of rows
        /// </summary>
        /// <param name="type"></param>
        /// <param name="rows"></param>
        /// <returns></returns>
        public static IEnumerable<object> FillList(Type type, IEnumerable<DataRow> rows)
        {

            List<object> list = new List<object>();
            foreach (DataRow row in rows)
            {
                object obj = Activator.CreateInstance(type);
                SetFromRow(obj, row);
                list.Add(obj);
            }
            return list;

        }

        /// <summary>
        /// Fills a list from a DataTable
        /// </summary>
        /// <param name="type"></param>
        /// <param name="table"></param>
        /// <returns></returns>
        public static IEnumerable<object> FillList(Type type, DataTable table)
        {

            return FillList(type, table.Select());

        }

        /// <summary>
        /// Fills a list from a list of rows
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="rows"></param>
        /// <returns></returns>
        public static IEnumerable<T> FillList<T>(IEnumerable<DataRow> rows)
        {

            return FillList(typeof(T), rows).Cast<T>().ToList();

        }

        /// <summary>
        /// Fills a list from a DataTable
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="table"></param>
        /// <returns></returns>
        public static IEnumerable<T> FillList<T>(DataTable table)
        {

            return FillList(typeof(T), table.Select()).Cast<T>().ToList();

        }

        /// <summary>
        /// Sets a properties of an object from a DataRow
        /// </summary>
        /// <param name="objectToSet"></param>
        /// <param name="row"></param>
        public static void SetFromRow(object objectToSet, DataRow row)
        {

            Type type = objectToSet.GetType();

            foreach (PropertyInfo prop in type.GetProperties().Where(p => p.CanWrite))
            {

                string propName = getName(prop);

                if (!row.Table.Columns.Contains(propName))
                    continue;

                DataColumn column = row.Table.Columns[propName];

                if (column == null)
                    continue;

                try
                {

                    object value = ChangeType(row[column], prop.PropertyType);
                    prop.SetValue(objectToSet, value);

                }
                catch
                {

                    throw new Exception(string.Format("Could not assign value to '{0}'.", propName));

                }

            }

        }

        /// <summary>
        /// Sets the properties of an object from a DataReader
        /// </summary>
        /// <param name="objectToSet"></param>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static object SetFromReader(object objectToSet, IDataReader reader)
        {

            Type type = objectToSet.GetType();

            List<string> columns = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToList();

            foreach (PropertyInfo prop in type.GetProperties().Where(p => p.CanWrite))
            {

                string propName;

                if (HasAttribute<ForeignColumn>(prop))
                    propName = prop.Name;
                else
                    propName = getName(prop);

                if (!columns.Contains(propName))
                    continue;

                object value = ChangeType(reader[propName], prop.PropertyType);
                prop.SetValue(objectToSet, value);


            }

            return objectToSet;

        }

        /// <summary>
        /// Creates an SHA256 hash of a string
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string SHA256(string value)
        {
            using (SHA256 hash = System.Security.Cryptography.SHA256.Create())
            {
                return string.Join("", hash
                  .ComputeHash(Encoding.UTF8.GetBytes(value))
                  .Select(item => item.ToString("x2")));
            }

        }

        /// <summary>
        /// Creates an MD5 hash of a string
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string MD5(string value)
        {

            using (MD5 hash = System.Security.Cryptography.MD5.Create())
            {
                return string.Join("", hash
                  .ComputeHash(Encoding.GetEncoding(1252).GetBytes(value))
                  .Select(item => item.ToString("x2")));
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static string MD5ListHash<T>(IEnumerable<T> list)
        {

            Type type = typeof(T);
            IEnumerable<PropertyInfo> props = type.GetProperties().Where(p => HasAttribute<Column>(p));

            List<string> rows = new List<string>();
            foreach (T item in list)
            {

                List<object> values = new List<object>();
                foreach (PropertyInfo prop in props)
                {
                    object v = prop.GetValue(item);
                    if (v != null && (prop.PropertyType == typeof(DateTime) || prop.PropertyType == typeof(DateTime?)))
                        values.Add(((DateTime)v).ToString("MM/dd/yyyy H:mm:ss"));
                    else
                        values.Add(v);
                }
                rows.Add(MD5(string.Join(",", values)).ToUpper());

            }

            string joined = string.Join(",", rows);
            return MD5(joined).ToUpper();

        }

        /// <summary>
        /// Compresses a string using GZip
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string CompressString(string text)
        {

            byte[] buffer = Encoding.UTF8.GetBytes(text);
            MemoryStream memoryStream = new MemoryStream();
            using (GZipStream gZipStream = new GZipStream(memoryStream, CompressionMode.Compress, true))
                gZipStream.Write(buffer, 0, buffer.Length);

            memoryStream.Position = 0;

            byte[] compressedData = new byte[memoryStream.Length];
            memoryStream.Read(compressedData, 0, compressedData.Length);

            byte[] gZipBuffer = new byte[compressedData.Length + 4];
            Buffer.BlockCopy(compressedData, 0, gZipBuffer, 4, compressedData.Length);
            Buffer.BlockCopy(BitConverter.GetBytes(buffer.Length), 0, gZipBuffer, 0, 4);
            return Convert.ToBase64String(gZipBuffer);

        }

        /// <summary>
        /// Decompresses a string using GZip
        /// </summary>
        /// <param name="compressedText"></param>
        /// <returns></returns>
        public static string DecompressString(string compressedText)
        {
            byte[] gZipBuffer = Convert.FromBase64String(compressedText);
            using (MemoryStream memoryStream = new MemoryStream())
            {
                int dataLength = BitConverter.ToInt32(gZipBuffer, 0);
                memoryStream.Write(gZipBuffer, 4, gZipBuffer.Length - 4);

                byte[] buffer = new byte[dataLength];

                memoryStream.Position = 0;
                using (GZipStream gZipStream = new GZipStream(memoryStream, CompressionMode.Decompress))
                    gZipStream.Read(buffer, 0, buffer.Length);

                return Encoding.UTF8.GetString(buffer);
            }
        }

        #endregion

        #region Internal

        internal void addParamObject(SqlCommand cmd, object parameters)
        {

            if (parameters != null)
                if (parameters is SqlParameterCollection)
                    foreach (SqlParameter p in (SqlParameterCollection)parameters)
                        cmd.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));
                else
                    foreach (PropertyInfo prop in parameters.GetType().GetProperties())
                        addParam(cmd, prop.Name, prop.GetValue(parameters));

        }

        internal void readList(SqlCommand cmd, Type type, List<object> list)
        {

            using (SqlDataReader reader = cmd.ExecuteReader())
                if (reader.HasRows)
                    while (reader.Read())
                        list.Add(readObject(type, reader));

        }

        internal T readSingle<T>(SqlCommand cmd)
        {

            using (SqlDataReader reader = cmd.ExecuteReader(CommandBehavior.SingleRow))
                if (reader.HasRows)
                {
                    reader.Read();
                    return (T)readObject(typeof(T), reader);
                }

            return default(T);

        }

        internal object readObject(Type type, SqlDataReader reader)
        {

            object obj = Activator.CreateInstance(type);
            SetFromReader(obj, reader);
            return obj;

        }

        internal void readScalarList<T>(SqlCommand cmd, List<T> list)
        {

            using (SqlDataReader reader = cmd.ExecuteReader())
                if (reader.HasRows)
                    while (reader.Read())
                        list.Add(reader.GetFieldValue<T>(0));

        }

        internal T readScalar<T>(SqlCommand cmd)
        {
            object v = cmd.ExecuteScalar();
            return v == DBNull.Value || v == null ? default(T) : (T)v;

        }

        internal void select(SqlConnection conn, SqlCommand cmd, string tableObjectName, List<MemberInfo> columns, List<MemberInfo> foreignColumns, int top, Expression where)
        {

            //get columns to select
            List<string> columnsToSelect = columns.Where(c => !HasAttribute<IgnoreOnSelect>(c)).Select(c => string.Format("{0}.{1}", tableObjectName, getObjectName(c))).ToList();
            if (columnsToSelect.Count == 0)
                throw new Exception("No columns to select");

            //generate join clause
            string joinClause = generateJoinClause(tableObjectName, columnsToSelect, foreignColumns);

            //generate where clause
            string whereClause = generateWhereClause(cmd, tableObjectName, where);

            //generate top clause
            string topClause = "";
            if (top > 0)
                topClause = string.Format("top {0}", top);

            //generate sql
            cmd.CommandText = "select {top} {columns} from {model} {joins} {where}".Inject(new
            {
                top = topClause,
                model = tableObjectName,
                columns = string.Join(", ", columnsToSelect),
                joins = joinClause,
                where = whereClause
            });

            //execute sql
            conn.Open();

        }

        internal int update<T>(TableDefinition def, object values, IEnumerable<MemberInfo> properties, int timeout, Expression<Func<T, bool>> where = null)
        {

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = conn.CreateCommand())
            {

                cmd.CommandTimeout = timeout < 0 ? CommandTimeout : timeout;

                //columns to select
                List<string> columnsToUpdate = new List<string>();
                List<string> primaryKeys = new List<string>();
                string whereClause = generateWhereClause(cmd, def.objectName, where);
                foreach (PropertyInfo prop in properties)
                {

                    if (!HasAttribute<IgnoreOnUpdate>(prop))
                    {
                        object v = prop.GetValue(values);
                        if (v is string && string.IsNullOrEmpty((string)v))
                            v = null;

                        SqlParameter param = addParam(cmd, "update_field_" + getGuidString(), v);
                        columnsToUpdate.Add(string.Format("{0}.{1} = {2}", def.objectName, getObjectName(prop), param.ParameterName));
                    }

                    if (HasAttribute<PrimaryKey>(prop) && where == null)
                    {
                        SqlParameter param = addParam(cmd, "where_" + getGuidString(), prop.GetValue(values));
                        primaryKeys.Add(string.Format("{0}.{1} = {2}", def.objectName, getObjectName(prop), param.ParameterName));
                    }

                }

                if (where == null)
                    whereClause = "where " + string.Join(" and ", primaryKeys);

                //generate sql
                cmd.CommandText = string.Format("update {0} set {1} {2}", def.objectName, string.Join(", ", columnsToUpdate), whereClause);

                //execute
                conn.Open();
                return cmd.ExecuteNonQuery();

            }

        }

        internal T insert<T>(Type model, object objectToInsert, int timeout)
        {

            TableDefinition def = getTable(model);

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandTimeout = timeout < 0 ? CommandTimeout : timeout;
                //get columns and values
                List<string> columns = new List<string>();
                List<string> values = new List<string>();
                foreach (PropertyInfo prop in objectToInsert.GetType().GetProperties())
                    if (def.columns.Contains(prop) && !HasAttribute<IgnoreOnInsert>(prop))
                    {

                        columns.Add(string.Format("{0}.{1}", def.objectName, getObjectName(prop)));

                        SqlParameter param = addParam(cmd, "insert_value_" + getGuidString(), prop.GetValue(objectToInsert));
                        values.Add(param.ParameterName);

                    }

                //get primary keys
                List<string> outputTableKeys = new List<string>();
                List<string> insertedPrimaryKeys = new List<string>();
                List<string> wherePrimaryKeys = new List<string>();
                foreach (PropertyInfo pk in def.primaryKeys)
                {
                    string primaryKeyName = getObjectName(pk);
                    outputTableKeys.Add(string.Format("{0} {1}", primaryKeyName, dbTypeMap[pk.PropertyType]));
                    insertedPrimaryKeys.Add("inserted." + primaryKeyName);
                    wherePrimaryKeys.Add("ids.{primaryKey} = {model}.{primaryKey}".Inject(new { primaryKey = primaryKeyName, model = def.objectName }));
                }

                //generate query
                cmd.CommandText = "{insertedTable};insert into {model} ({columns}) output {insertedPrimaryKeys} into @ids values ({values});select {model}.* from {model} join @ids ids on {wherePrimaryKeys}".Inject(new
                {
                    insertedTable = string.Format("declare @ids table({0})", string.Join(", ", outputTableKeys)),
                    model = def.objectName,
                    columns = string.Join(", ", columns),
                    insertedPrimaryKeys = string.Join(", ", insertedPrimaryKeys),
                    values = string.Join(", ", values),
                    wherePrimaryKeys = string.Join(" and ", wherePrimaryKeys)
                });

                conn.Open();

                if (HasAttribute<Table>(typeof(T)))
                    return readSingle<T>(cmd);
                else
                    return readScalar<T>(cmd);

            }

        }

        internal TableDefinition getTable(Type type)
        {

            if (tables.ContainsKey(type))
                return tables[type];

            TableDefinition table = new TableDefinition();
            table.objectName = getObjectName(type);
            table.type = type;

            foreach (var prop in type.GetProperties())
            {

                if (HasAttribute<Column>(prop))
                    table.columns.Add(prop);

                if (HasAttribute<ForeignColumn>(prop))
                    table.foreignColumns.Add(prop);

                if (HasAttribute<PrimaryKey>(prop))
                    table.primaryKeys.Add(prop);

            }

            if (table.columns.Count == 0 && table.primaryKeys.Count == 0)
                throw new Exception(string.Format("Model '{0}' has no columns defined.", type));

            tables.Add(type, table);

            return table;

        }

        /// <summary>
        /// Retrieves the name of member
        /// </summary>
        /// <param name="member"></param>
        /// <returns></returns>
        public static string getName(MemberInfo member)
        {

            string name = member.Name;
            if (HasAttribute<OverrideName>(member))
                name = member.GetCustomAttribute<OverrideName>().name;

            return name;

        }

        /// <summary>
        /// Retrieves object name of member
        /// </summary>
        /// <param name="member"></param>
        /// <returns></returns>
        public static string getObjectName(MemberInfo member)
        {

            return getObjectName(getName(member));

        }

        internal static string getObjectName(string name)
        {

            return string.Format("[{0}]", name);

        }

        internal static string getGuidString()
        {

            return Guid.NewGuid().ToString("n");

        }

        internal static SqlParameter addParam(SqlCommand cmd, string name, object value)
        {

            if (value is DateTime && (DateTime)value < SqlDateTime.MinValue.Value)
                value = SqlDateTime.MinValue.Value;

            SqlParameter param = cmd.CreateParameter();
            param.ParameterName = "@" + name;
            param.Value = value ?? Convert.DBNull;

            return cmd.Parameters.Add(param);

        }

        internal static string addWhereParam(SqlCommand cmd, object value)
        {

            return value == null ? "null" : addParam(cmd, string.Format("where_{0}", getGuidString()), value).ParameterName;

        }

        internal string generateWhereClause(SqlCommand cmd, string tableObjectName, Expression where, bool addWhere = true)
        {

            if (where == null)
                return null;

            SQLExpressionTranslator translater = new SQLExpressionTranslator();
            return (addWhere ? "where " : "") + translater.GenerateSQLExpression(this, cmd, where, tableObjectName);

        }

        internal string generateJoinClause(string tableObjectName, List<string> columnsToSelect, List<MemberInfo> foreignColumns)
        {

            if (foreignColumns == null || foreignColumns.Count == 0)
                return null;

            List<string> joinClauseList = new List<string>();
            foreach (PropertyInfo foreignColumn in foreignColumns)
            {

                ForeignColumn attr = foreignColumn.GetCustomAttribute<ForeignColumn>();

                string otherTableObjectName = getObjectName(attr.otherTableModel);
                string alias = getObjectName(string.Format("join_table_{0}", getGuidString()));
                string foreignField = getObjectName(foreignColumn);

                string joinPart = "join";
                if (attr.joinType == JoinType.LEFT)
                    joinPart = "left join";
                else if (attr.joinType == JoinType.RIGHT)
                    joinPart = "right join";
                else if (attr.joinType == JoinType.FULL_OUTER)
                    joinPart = "full outer join";

                joinClauseList.Add("{joinPart} {otherModel} as {alias} on {model}.{key} = {alias}.{otherKey}".Inject(new
                {

                    joinPart = joinPart,
                    model = tableObjectName,
                    otherModel = otherTableObjectName,
                    alias = alias,
                    key = attr.KeyInThisTableModel,
                    otherKey = attr.KeyInOtherTableModel
                }));

                columnsToSelect.Add(string.Format("{0}.{1} as {2}",
                    alias,
                    attr.FieldInOtherTableModel ?? foreignField,
                    foreignField));

            }

            return string.Join("\r\n", joinClauseList);



        }

        internal static readonly Dictionary<Type, string> dbTypeMap = new Dictionary<Type, string>
        {
            {typeof(Int64), "bigint"},
            {typeof(UInt64), "bigint"},

            {typeof(Byte[]), "varbinary"},
            {typeof(Boolean), "bit"},
            {typeof(String), "nvarchar(max)"},
            {typeof(Char), "nchar"},
            {typeof(DateTime), "datetime"},
            {typeof(DateTimeOffset), "datetimeoffset"},
            {typeof(Decimal), "decimal"},
            {typeof(Double), "float"},
            {typeof(Int32), "int"},
            {typeof(UInt32), "int"},
            {typeof(TimeSpan), "time"},
            {typeof(Guid), "uniqueidentifier"},
            {typeof(Int16), "smallint"},
            {typeof(UInt16), "smallint"},
            {typeof(Single), "real"},

            {typeof(Int64?), "bigint"},
            {typeof(UInt64?), "bigint"},
            {typeof(Byte?[]), "varbinary"},
            {typeof(Boolean?), "bit"},
            {typeof(Char?), "nchar"},
            {typeof(DateTime?), "date"},
            {typeof(DateTimeOffset?), "datetimeoffset"},
            {typeof(Decimal?), "decimal"},
            {typeof(Double?), "float"},
            {typeof(Int32?), "int"},
            {typeof(UInt32?), "int"},
            {typeof(TimeSpan?), "time"},
            {typeof(Guid?), "uniqueidentifier"},
            {typeof(Int16?), "smallint"},
            {typeof(UInt16?), "smallint"},
            {typeof(Single?), "real"}

        };

        #endregion

    }
}
