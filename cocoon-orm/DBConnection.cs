
using Cocoon.Annotations;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web;
using System.Linq.Expressions;

namespace Cocoon
{

    /// <summary>
    /// The database connection class
    /// </summary>
    public class DBConnection
    {

        /// <summary>
        /// The connection string cocoon is using
        /// </summary>
        public string ConnectionString;

        internal Dictionary<Type, TableDefinition> tableDefinitions = new Dictionary<Type, TableDefinition>();

        /// <summary>
        /// 
        /// </summary>
        public DBServerAdapter adapter;

        private Action<string> loggerMethod;

        #region Public Interface

        /// <summary>
        /// Creates a new database connection
        /// </summary>
        /// <param name="connectionString">The connection string to use to connect to the database</param>
        /// <param name="dataBaseAdapter">An instance of a database adapter</param>
        /// <param name="logMethod">A method to call for logging purposes</param>
        public DBConnection(string connectionString, DBServerAdapter dataBaseAdapter, Action<string> logMethod = null)
        {

            loggerMethod = logMethod;

            adapter = dataBaseAdapter;
            adapter.connection = this;

            ConnectionString = connectionString;

        }

        /// <summary>
        /// Creates a new SQLServer database connection
        /// </summary>
        /// <param name="connectionString">The connection string to use to connect to the database</param>
        /// <param name="logMethod">A method to call for logging purposes</param>
        public DBConnection(string connectionString, Action<string> logMethod = null)
        {
            loggerMethod = logMethod;

            adapter = new SQLServerAdapter();
            adapter.connection = this;

            ConnectionString = connectionString;

        }

        #region Stored Procedures

        /// <summary>
        /// Executes a stored procedure that returns a single object of type T
        /// </summary>
        /// <typeparam name="T">The object model/table to get the item from</typeparam>
        /// <param name="procedureName">The name of the stored procedure to call</param>
        /// <param name="paramObject">An object containing the parameters to pass to the stored procedure</param>
        /// <returns>An object of type T</returns>
        public T ExecuteSProcSingle<T>(string procedureName, object paramObject = null)
        {
            List<T> list = ExecuteSProcList<T>(procedureName, paramObject);
            if (list != null && list.Count > 0)
                return list[0];
            else
                return default(T);
        }

        /// <summary>
        /// Executes a stored procedure that returns a list of type T
        /// </summary>
        /// <typeparam name="T">The object model/table to select items from</typeparam>
        /// <param name="procedureName">The name of the stored procedure to call</param>
        /// <param name="paramObject">An object containing the parameters to pass to the stored procedure</param>
        /// <param name="fieldToMap">A field to map to the list, if null the first field is used</param>
        /// <returns>A list of items of type T</returns>
        public List<T> ExecuteSProcList<T>(string procedureName, object paramObject = null, string fieldToMap = null)
        {

            Type type = typeof(T);

            int rowsFilled;
            DataSet ds = executeProcForDataSet(procedureName, paramObject, out rowsFilled);

            if (rowsFilled == 0)
                return new List<T>();

            bool isTable = Utilities.HasAttribute<Table>(type);

            //fill list
            List<T> list = new List<T>();
            foreach (DataRow row in ds.Tables[0].Rows)
            {

                if (isTable && fieldToMap == null)
                {

                    T item = (T)Activator.CreateInstance(type);
                    Utilities.SetFromRow(item, row);
                    list.Add(item);

                }
                else
                {

                    if (fieldToMap == null)
                        list.Add((T)row[0]);
                    else if (row.Table.Columns.Contains(fieldToMap))
                        list.Add((T)row[fieldToMap]);

                }
            }

            return list;

        }

        /// <summary>
        /// Executes a stored procedure that returns a DataSet
        /// </summary>
        /// <param name="procedureName">The name of the stored procedure to call</param>
        /// <param name="paramObject">An object containing the parameters to pass to the stored procedure</param>
        /// <returns></returns>
        public DataSet ExecuteSProcDataSet(string procedureName, object paramObject = null)
        {

            int rowsFilled;
            DataSet ds = executeProcForDataSet(procedureName, paramObject, out rowsFilled);

            if (rowsFilled == 0)
                return null;
            else
                return ds;

        }

        /// <summary>
        /// Executes a stored procedure that returns nothing.
        /// </summary>
        /// <param name="procedureName">The name of the stored procedure to call</param>
        /// <param name="paramObject">An object containing the parameters to pass to the stored procedure</param>
        /// <returns>The number of rows affected if NOCOUNT OFF</returns>
        public int ExecuteSProcVoid(string procedureName, object paramObject = null)
        {

            using (var connection = adapter.getConnection(ConnectionString))
            {

                adapter.openSProc(connection, procedureName);

                adapter.addSProcParams(connection, paramObject);

                return connection.command.ExecuteNonQuery();

            }

        }

        /// <summary>
        /// Executes a stored procedure that returns one scalar value.
        /// </summary>
        /// <param name="procedureName">The name of the stored procedure to call</param>
        /// <param name="paramObject">An object containing the parameters to pass to the stored procedure</param>
        /// <returns>The scalar value of type T</returns>
        public T ExecuteSProcScalar<T>(string procedureName, object paramObject = null)
        {

            using (var connection = adapter.getConnection(ConnectionString))
            {

                adapter.openSProc(connection, procedureName);

                adapter.addSProcParams(connection, paramObject);

                return (T)connection.command.ExecuteScalar();

            }

        }

        #endregion

        #region SQL

        /// <summary>
        /// Executes SQL statements and returns a single object of type T. Use @params in your SQL.
        /// </summary>
        /// <typeparam name="T">The object model/table to map the result to</typeparam>
        /// <param name="sql">Your SQL command string</param>
        /// <param name="paramObject">The object to map parameters from</param>
        /// <returns>An object of type T</returns>
        public T ExecuteSQLSingle<T>(string sql, object paramObject = null)
        {
            List<T> list = ExecuteSQLList<T>(sql, paramObject);
            if (list != null && list.Count > 0)
                return list[0];
            else
                return default(T);
        }

        /// <summary>
        /// Executes SQL statements on the database. Use @params in your SQL.
        /// </summary>
        /// <typeparam name="T">The object model/table to map the result to</typeparam>
        /// <param name="sql">Your SQL command string</param>
        /// <param name="paramObject">The object to map parameters from</param>
        /// <param name="fieldToMap">A field to map to the list, if null the first field is used</param>
        /// <returns>A list of items of type T</returns>
        public List<T> ExecuteSQLList<T>(string sql, object paramObject = null, string fieldToMap = null)
        {

            //get type
            Type type = typeof(T);

            DataSet ds = ExecuteSQLDataSet(sql, paramObject);

            if (ds == null)
                return new List<T>();

            //fill list
            List<T> list = new List<T>();
            foreach (DataRow row in ds.Tables[0].Rows)
            {

                if (Utilities.HasAttribute<Table>(type))
                {

                    T item = (T)Activator.CreateInstance(type);
                    Utilities.SetFromRow(item, row);
                    list.Add(item);

                }
                else
                {

                    if (fieldToMap == null)
                        list.Add((T)row[0]);
                    else if (row.Table.Columns.Contains(fieldToMap))
                        list.Add((T)row[fieldToMap]);

                }

            }

            return list;

        }

        /// <summary>
        /// Executes a SQL command string that returns a DataSet. Use @params in your SQL.
        /// </summary>
        /// <param name="sql">Your SQL command string</param>
        /// <param name="paramObject">The object to map parameters from</param>
        /// <returns></returns>
        public DataSet ExecuteSQLDataSet(string sql, object paramObject = null)
        {

            using (var connection = adapter.getConnection(ConnectionString))
            {

                adapter.openSQL(connection, sql);

                adapter.addParams(connection, paramObject, "");

                DataSet ds;
                int rowsFilled = adapter.fillDataSet(connection, out ds);

                if (rowsFilled == 0)
                    return null;
                else
                    return ds;

            }

        }

        /// <summary>
        /// Executes a SQL command string that returns no rows. Use @params in your SQL.
        /// </summary>
        /// <param name="sql">Your SQL command string</param>
        /// <param name="paramObject">The object to map parameters from</param>
        /// <returns>The number of rows affected</returns>
        public int ExecuteSQLVoid(string sql, object paramObject = null)
        {

            using (var connection = adapter.getConnection(ConnectionString))
            {

                adapter.openSQL(connection, sql);

                adapter.addParams(connection, paramObject, "");

                return connection.command.ExecuteNonQuery();

            }

        }

        /// <summary>
        /// Executes a SQL command string that returns one scalar value. Use @params in your SQL.
        /// </summary>
        /// <typeparam name="T">The type of the scalar value</typeparam>
        /// <param name="sql">Your SQL command string</param>
        /// <param name="paramObject">The object to map parameters from</param>
        /// <returns>The scalar value of type T</returns>
        public T ExecuteSQLScalar<T>(string sql, object paramObject = null)
        {

            using (var connection = adapter.getConnection(ConnectionString))
            {

                adapter.openSQL(connection, sql);

                adapter.addParams(connection, paramObject, "");

                return (T)connection.command.ExecuteScalar();

            }

        }

        #endregion

        #region Simple CRUD API

        /// <summary>
        /// Retrieves a single item from the database
        /// </summary>
        /// <typeparam name="T">The object model/table to get the item from</typeparam>
        /// <param name="where">A where clause to add to the select</param>
        /// <param name="useOrLogic">If true, the where clause will use OR logic instead of AND logic</param>
        /// <returns>An object of type T</returns>
        public T GetSingle<T>(object where, bool useOrLogic = false)
        {

            if (where == null)
                throw new Exception("Where clause must be supplied.");

            //get type
            Type type = typeof(T);

            //do select
            DataSet ds;
            int rowsFilled;
            select(type, 1, where, useOrLogic, out ds, out rowsFilled);

            if (rowsFilled == 0)
                return default(T);

            //fill our object
            T obj = (T)Activator.CreateInstance(type);
            Utilities.SetFromRow(obj, ds.Tables[0].Rows[0]);

            return obj;

        }
        
        /// <summary>
        /// Returns a list of T objects
        /// </summary>
        /// <typeparam name="T">The object model/table to select items from</typeparam>
        /// <param name="where">A where clause to add to the select</param>
        /// <param name="useOrLogic">If true, the where clause will use OR logic instead of AND logic</param>
        /// <param name="top">If greater than 0, selects the top N items</param>
        /// <returns>A list of items of type T</returns>
        public List<T> GetList<T>(object where = null, bool useOrLogic = false, int top = 0)
        {
            List<object> list = GetList(typeof(T), where, useOrLogic, top);
            List<T> newList = new List<T>();
            foreach (object item in list)
                newList.Add((T)item);

            return newList;
            
        }

        public List<object> GetList(Type type, object where = null, bool useOrLogic = false, int top = 0)
        {
            
            //do select
            DataSet ds;
            int rowsFilled;
            select(type, top, where, useOrLogic, out ds, out rowsFilled);

            if (rowsFilled == 0)
                return new List<object>();

            //fill list
            List<object> list = new List<object>();
            foreach (DataRow row in ds.Tables[0].Rows)
            {
                object obj = Activator.CreateInstance(type);
                Utilities.SetFromRow(obj, row);
                list.Add(obj);
            }

            return list;

        }

        /// <summary>
        /// Returns a single scalar value from a table.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="tableName">The name of the table to retrieve the scalar from</param>
        /// <param name="scalarField">The name of the column in the table that contains the scalar</param>
        /// <param name="where">A where clause to add to the select</param>
        /// <param name="useOrLogic">If true, the where clause will use OR logic instead of AND logic</param>
        /// <returns>The scalar value of type T</returns>
        public T GetScalar<T>(string tableName, string scalarField, object where, bool useOrLogic = false)
        {

            if (where == null)
                throw new Exception("Where clause must be supplied.");

            string whereClause = generateWhereClause(tableName, where, useOrLogic, "where_");

            string sql = string.Format("select {0} from {1} where {2}", scalarField, tableName, whereClause);

            log("Generated SQL (GetScalar) " + sql);

            using (var connection = adapter.getConnection(ConnectionString))
            {

                connection.command.CommandText = sql;

                adapter.addParams(connection, where, "where_");

                connection.connection.Open();
                return (T)connection.command.ExecuteScalar();
            }

        }

        /// <summary>
        /// Returns a single scalar value from a table.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="objectModel">The table to retrieve the scalar from</param>
        /// <param name="scalarField">The name of the column in the table that contains the scalar</param>
        /// <param name="where">A where clause to add to the select</param>
        /// <param name="useOrLogic">If true, the where clause will use OR logic instead of AND logic</param>
        /// <returns>The scalar value of type T</returns>
        public T GetScalar<T>(Type objectModel, string scalarField, object where, bool useOrLogic = false)
        {

            TableDefinition def = getDef(objectModel);

            checkWhereForMultiTenantID(def, where);

            return GetScalar<T>(def.tableName, scalarField, where, useOrLogic);

        }

        /// <summary>
        /// Retrieves a list of scalar values from a column of a table
        /// </summary>
        /// <typeparam name="T">The type of item in the list</typeparam>
        /// <param name="tableName">The name of the table in the database</param>
        /// <param name="where">A where clause to add to the select</param>
        /// <param name="fieldToMap">A field to map to the list, if null the first field is used</param>
        /// <param name="useOrLogic">If true, the where clause will use OR logic instead of AND logic</param>
        /// <param name="top">If greater than 0, selects the top N items</param>
        /// <returns>A flat list of items of type T</returns>
        public List<T> GetScalarList<T>(string tableName, object where = null, string fieldToMap = null, bool useOrLogic = false, int top = 0)
        {

            //generate where clause
            string whereClause = "";
            if (where != null)
                whereClause = "where " + generateWhereClause(tableName, where, useOrLogic, "where_");

            //generate top
            string topClause = "";
            if (top > 0)
                topClause = string.Format("top {0}", top);

            //generate sql
            string sql;
            if (fieldToMap != null)
                sql = string.Format("select {0} {1} from {2} {3}", topClause, fieldToMap, tableName, whereClause);
            else
                sql = string.Format("select {0} * from {1} {2}", topClause, tableName, whereClause);

            log("Generated SQL (GetScalarList) " + sql);

            using (var connection = adapter.getConnection(ConnectionString))
            {

                connection.command.CommandText = sql;

                adapter.addParams(connection, where, "where_");

                //fill data set
                DataSet ds;
                int rowsFilled = adapter.fillDataSet(connection, out ds);

                if (rowsFilled == 0)
                    return new List<T>();

                //fill list
                return Utilities.FillScalarList<T>(ds.Tables[0], fieldToMap);

            }

        }

        /// <summary>
        /// Retrieves a list of scalar values from a column of a table
        /// </summary>
        /// <typeparam name="T">The type of item in the list</typeparam>
        /// <param name="objectModel">The table in the database</param>
        /// <param name="where">A where clause to add to the select</param>
        /// <param name="fieldToMap">A field to map to the list, if null the first field is used</param>
        /// <param name="useOrLogic">If true, the where clause will use OR logic instead of AND logic</param>
        /// <param name="top">If greater than 0, selects the top N items</param>
        /// <returns>A flat list of items of type T</returns>
        public List<T> GetScalarList<T>(Type objectModel, object where = null, string fieldToMap = null, bool useOrLogic = false, int top = 0)
        {
            TableDefinition def = getDef(objectModel);

            checkWhereForMultiTenantID(def, where);

            return GetScalarList<T>(def.tableName, where, fieldToMap, useOrLogic, top);

        }

        /// <summary>
        /// Deletes an object in the database
        /// </summary>
        /// <param name="tableName">The name of the table to delete rows from</param>
        /// <param name="where">A where clause to add to the delete</param>
        /// <param name="useOrLogic">If true, the where clause will use OR logic instead of AND logic</param>
        /// <returns></returns>
        public int Delete(string tableName, object where, bool useOrLogic = false)
        {

            if (where == null)
                throw new Exception("Where clause must be supplied.");

            string whereClause;

            if (where is string)
                whereClause = where.ToString();
            else
                whereClause = generateWhereClause(tableName, where, useOrLogic, "where_");

            string sql = string.Format("delete from {0} where {1}", adapter.getObjectName(tableName), whereClause);

            log("Generated SQL (Delete) " + sql);

            using (var connection = adapter.getConnection(ConnectionString))
            {

                //set sql
                connection.command.CommandText = sql;

                //add parameters
                adapter.addParams(connection, where, "where_");

                //execute
                connection.connection.Open();

                return connection.command.ExecuteNonQuery();

            }

        }

        /// <summary>
        /// Deletes an object in the database
        /// </summary>
        /// <param name="objectModel">The object model/table to delete a record from</param>
        /// <param name="where">A where clause to add to the delete</param>
        /// <param name="useOrLogic">If true, the where clause will use OR logic instead of AND logic</param>
        /// <returns>The number of rows affected by the delete</returns>
        public int Delete(Type objectModel, object where, bool useOrLogic = false)
        {

            if (where == null)
                throw new Exception("Where clause must be supplied.");

            TableDefinition def = getDef(objectModel);

            if (where is string == false) 
                checkWhereForMultiTenantID(def, where);
     
            return Delete(def.tableName, where, useOrLogic);

        }

        /// <summary>
        /// Deletes an object in the database
        /// </summary>
        /// <param name="objectToDelete">The object model/record to delete from the database</param>
        /// <param name="where">A where clause to add to the delete. If null the primary key fields of the table are used</param>
        /// <param name="useOrLogic">If true, the where clause will use OR logic instead of AND logic</param>
        /// <returns>The number of rows affected by the delete</returns>
        public int Delete(object objectToDelete, object where = null, bool useOrLogic = false)
        {

            TableDefinition def = getDef(objectToDelete.GetType());

            if (where == null && def.primaryKeys.Count == 0)
                throw new Exception("Where clause must be supplied when no primary keys exist in table.");

            //generate where clause
            string whereClause = "";
            bool usePrimaryKeysInWhereClause = false;

            if (where != null)
            {

                if (where is string)
                    whereClause = (string)where;
                else
                {
                    checkWhereForMultiTenantID(def, where);
                    whereClause = generateWhereClause(def.tableName, where, useOrLogic, "where_");
                }
            }
            else if (def.primaryKeys.Count > 0)
            {

                if (def.multiTenantIDColumns.Count > 0)
                    multiTenantMatch(def, def.primaryKeys.ToArray());

                usePrimaryKeysInWhereClause = true;
                whereClause = generateWhereClause(def.tableName, def.primaryKeys, objectToDelete, useOrLogic, "where_");
            }

            string sql = string.Format("delete from {0} where {1}", def.objectName, whereClause);

            log("Generated SQL (Delete) " + sql);

            using (var connection = adapter.getConnection(ConnectionString))
            {

                //set sql
                connection.command.CommandText = sql;

                //add parameters
                if (usePrimaryKeysInWhereClause)
                    adapter.addParams(connection, def.primaryKeys, objectToDelete, "where_");
                else
                    adapter.addParams(connection, where, "where_");

                //execute
                connection.connection.Open();

                return connection.command.ExecuteNonQuery();

            }
            
        }

        /// <summary>
        /// Updates and existing record in the database
        /// </summary>
        /// <param name="objectToUpdate">The object to use as values for the update</param>
        /// <param name="where">A where clause to add to the update</param>
        /// <param name="useOrLogic">If true, the where clause will use OR logic instead of AND logic</param>
        /// <returns>The number of rows affected by the update</returns>
        public int Update(object objectToUpdate, object where = null, bool useOrLogic = false)
        {
            TableDefinition def = getDef(objectToUpdate.GetType());

            return update(
                def,
                def.allColumns,
                objectToUpdate,
                where,
                useOrLogic);

        }

        /// <summary>
        /// Updates and existing record in the database
        /// </summary>
        /// <param name="objectModel">The object model/table to update</param>
        /// <param name="fieldsToUpdate">The object to use as values for the update</param>
        /// <param name="where">A where clause to add to the update</param>
        /// <param name="useOrLogic">If true, the where clause will use OR logic instead of AND logic</param>
        /// <returns>The number of rows affected by the update</returns>
        public int Update(Type objectModel, object fieldsToUpdate, object where = null, bool useOrLogic = false)
        {

            return update(
                getDef(objectModel),
                new List<PropertyInfo>(fieldsToUpdate.GetType().GetProperties()),
                fieldsToUpdate,
                where,
                useOrLogic);

        }
        
        /// <summary>
        /// Inserts a list of records into the database and returns a list of the inserted objects.
        /// </summary>
        /// <typeparam name="T">The type of objects to return</typeparam>
        /// <param name="listToInsert">The list of objects to use for the insert</param>
        /// <returns>A list of the inserted objects</returns>
        public IEnumerable<T> Insert<T>(IEnumerable<T> listToInsert)
        {

            if (listToInsert.Count() == 0)
                throw new Exception("List to insert cannot be empty.");

            Type returnType = typeof(T);
            TableDefinition def = getDef(returnType);

            //get columns and values
            List<string> columns;
            List<string> notUsedValues;

            List<PropertyInfo> primaryKeys;
            List<PropertyInfo> parameters;
            insertColumns(def, out columns, out notUsedValues, out primaryKeys, out parameters);

            //generate sql
            List<string> insertParts = new List<string>();

            insertParts.Add(adapter.insertInitSQL(def.tableName, primaryKeys));

            List<string> whereParts = new List<string>();
            for (int i = 0; i < listToInsert.Count(); i++)
            {

                List<string> values = new List<string>();
                List<string> innerWhereParts = new List<string>();
                foreach (PropertyInfo prop in def.allColumns)
                    if (!Utilities.HasAttribute<IgnoreOnInsert>(prop))
                    {
                        string propName = Utilities.GetColumnName(prop);
                        string valueParam = adapter.getParamName(string.Format("value_{0}_{1}", i, propName));

                        values.Add(valueParam);

                        if (primaryKeys.Contains(prop))
                            innerWhereParts.Add(string.Format("{0}.{1} = {2}", adapter.getObjectName(def.tableName), propName, valueParam));

                    }

                whereParts.Add(string.Format("({0})", string.Join(" and ", innerWhereParts)));

                insertParts.Add(adapter.insertSQL(def.tableName, columns, values, primaryKeys));

            }

            insertParts.Add(adapter.insertSelectSQL(def.tableName, string.Join(" or ", whereParts), primaryKeys));

            insertParts.RemoveAll(s => s == null);
            string sql = string.Join(";", insertParts);

            log("Generated SQL (Insert) " + sql);

            using (var connection = adapter.getConnection(ConnectionString))
            {

                //set sql
                adapter.openSQL(connection, sql);

                //add parameters
                for (int i = 0; i < listToInsert.Count(); i++)
                    adapter.addParams(connection, parameters, listToInsert.ElementAt(i), string.Format("value_{0}_", i));

                //get data
                DataSet ds;
                int rowsFilled = adapter.fillDataSet(connection, out ds);

                if (rowsFilled == 0)
                    return default(IEnumerable<T>);

                List<T> returnList = new List<T>();
                foreach (DataRow row in ds.Tables[0].Rows)
                {

                    T item = (T)Activator.CreateInstance(returnType);
                    Utilities.SetFromRow(item, row);
                    returnList.Add(item);

                }

                return returnList;

            }

        }

        /// <summary>
        /// Inserts a new record into the database and returns an output object.
        /// </summary>
        /// <typeparam name="T">The type of object to return</typeparam>
        /// <param name="objectToInsert">The object to use for the insert</param>
        /// <returns></returns>
        public T Insert<T>(object objectToInsert)
        {
            return (T)Insert(objectToInsert, typeof(T));
        }

        /// <summary>
        /// Inserts a new record into the database and returns an output object.
        /// </summary>
        /// <param name="objectToInsert">The object to use for the insert</param>
        /// <param name="returnType">The type of object to return</param>
        /// <returns></returns>
        public object Insert(object objectToInsert, Type returnType)
        {

            TableDefinition def = getDef(objectToInsert.GetType());
            
            //get columns and values
            List<string> columns;
            List<string> values;
            List<PropertyInfo> primaryKeys;
            List<PropertyInfo> parameters;
            insertColumns(def, out columns, out values, out primaryKeys, out parameters);

            List<string> wherePrimaryKeys = new List<string>();
            foreach (PropertyInfo pk in primaryKeys)
            {

                string propName = Utilities.GetColumnName(pk);
                wherePrimaryKeys.Add(string.Format("{0}.{1} = {2}", def.tableName, adapter.getObjectName(propName), adapter.getParamName("value_" + propName)));

            }

            //put together sql
            List<string> insertParts = new List<string>();
            insertParts.Add(adapter.insertInitSQL(def.tableName, primaryKeys));
            insertParts.Add(adapter.insertSQL(def.tableName, columns, values, primaryKeys));
            insertParts.Add(adapter.insertSelectSQL(def.tableName, string.Join(" and ", wherePrimaryKeys), primaryKeys));
            string sql = string.Join(";", insertParts);



            log("Generated SQL (Insert) " + sql);

            using (var connection = adapter.getConnection(ConnectionString))
            {

                //set sql
                adapter.openSQL(connection, sql);

                //add parameters
                adapter.addParams(connection, parameters, objectToInsert, "value_");

                //get return
                if (Utilities.HasAttribute<Table>(returnType))
                {

                    DataSet ds;
                    int rowsFilled = adapter.fillDataSet(connection, out ds);

                    if (rowsFilled == 0)
                        return null;

                    object objectToReturn = Activator.CreateInstance(returnType);
                    Utilities.SetFromRow(objectToReturn, ds.Tables[0].Rows[0]);

                    return objectToReturn;

                }
                else
                {

                    return connection.command.ExecuteScalar();

                }

            }

        }

        #endregion

        #region Debug API

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataSet"></param>
        /// <returns></returns>
        public string GetCSV(DataSet dataSet)
        {

            StringBuilder result = new StringBuilder();

            foreach (DataTable table in dataSet.Tables)
            {

                for (int i = 0; i < table.Columns.Count; i++)
                {
                    result.Append(table.Columns[i].ColumnName);
                    result.Append(i == table.Columns.Count - 1 ? "\n" : ",");
                }

                foreach (DataRow row in table.Rows)
                {
                    for (int i = 0; i < table.Columns.Count; i++)
                    {
                        result.Append("\"" + row[i].ToString().Replace("\"", "\"\"") + "\"");
                        result.Append(i == table.Columns.Count - 1 ? "\n" : ",");
                    }
                }

            }

            return result.ToString();

        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public string GetCSV<T>(List<T> list)
        {

            Type type = typeof(T);

            StringBuilder result = new StringBuilder();

            PropertyInfo[] props = type.GetProperties();

            for (int i = 0; i < props.Length; i++)
            {
                result.Append(props[i].Name);
                result.Append(i == props.Length - 1 ? "\n" : ",");
            }

            foreach (T item in list)
            {
                for (int i = 0; i < props.Length; i++)
                {
                    result.Append(props[i].GetValue(item));
                    result.Append(i == props.Length - 1 ? "\n" : ",");
                }
            }

            return result.ToString();

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataSet"></param>
        /// <returns></returns>
        public string GetHTML(DataSet dataSet)
        {
            StringBuilder result = new StringBuilder();

            foreach (DataTable table in dataSet.Tables)
            {

                result.Append("<table>");

                result.Append("<tr>");
                for (int i = 0; i < table.Columns.Count; i++)
                    result.Append("<th>" + table.Columns[i].ColumnName + "</th>");
                result.Append("</tr>");


                foreach (DataRow row in table.Rows)
                {
                    result.Append("<tr>");
                    for (int i = 0; i < table.Columns.Count; i++)
                        result.Append("<td>" + row[i].ToString() + "</td>");
                    result.Append("</tr>");
                }

                result.Append("</table>");

            }

            return result.ToString();

        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public string GetHTML<T>(List<T> list)
        {

            Type type = typeof(T);

            StringBuilder result = new StringBuilder();

            result.Append("<table>");

            PropertyInfo[] props = type.GetProperties();

            result.Append("<tr>");
            foreach (PropertyInfo prop in props)
                result.Append("<th>" + prop.Name + "</th>");
            result.Append("</tr>");


            foreach (T item in list)
            {
                result.Append("<tr>");
                foreach (PropertyInfo prop in props)
                {

                    object v = prop.GetValue(item);

                    if (v == null)
                        result.Append("<td>null</td>");
                    else
                        result.Append("<td>" + HttpUtility.HtmlEncode(v.ToString()) + "</td>");

                }
                result.Append("</tr>");
            }

            result.Append("</table>");

            return result.ToString();

        }

        #endregion

        #endregion

        #region Private Implementation Methods

        private void insertColumns(TableDefinition def, out List<string> columns, out List<string> values, out List<PropertyInfo> primaryKeys, out List<PropertyInfo> parameters)
        {

            columns = new List<string>();
            values = new List<string>();
            primaryKeys = new List<PropertyInfo>();
            parameters = new List<PropertyInfo>();
            foreach (PropertyInfo prop in def.allColumns)
            {
                if (!Utilities.HasAttribute<IgnoreOnInsert>(prop))
                {
                    string propName = Utilities.GetColumnName(prop);
                    columns.Add(string.Format("{0}.{1}", def.objectName, adapter.getObjectName(propName)));
                    values.Add(adapter.getParamName(string.Format("value_{0}", propName)));
                    parameters.Add(prop);
                }

                if (Utilities.HasAttribute<PrimaryKey>(prop))
                    primaryKeys.Add(prop);

            }

        }

        private int update(TableDefinition def, List<PropertyInfo> fieldsToUpdate, object values, object where = null, bool useOrLogic = false)
        {

            //generate where clause
            string whereClause = "";
            bool usePrimaryKeysInWhereClause = false;

            if (where != null)
            {

                if (where is string)
                    whereClause = "where " + where;
                else
                {
                    checkWhereForMultiTenantID(def, where);
                    whereClause = "where " + generateWhereClause(def.tableName, where, useOrLogic, "where_");
                }
            }
            else if (def.primaryKeys.Count > 0)
            {

                if (def.multiTenantIDColumns.Count > 0)
                    multiTenantMatch(def, def.primaryKeys.ToArray());

                usePrimaryKeysInWhereClause = true;
                whereClause = "where " + generateWhereClause(def.tableName, def.primaryKeys, values, useOrLogic, "where_");
            }

            //generate columns
            List<string> columnsToUpdate = new List<string>();
            List<PropertyInfo> columnsToUpdateParams = new List<PropertyInfo>();
            foreach (PropertyInfo field in fieldsToUpdate)
                if (!Utilities.HasAttribute<IgnoreOnUpdate>(field))
                {
                    columnsToUpdate.Add(generateAssignmentClause(def.tableName, field, values, "update_", false));
                    columnsToUpdateParams.Add(field);
                }

            string sql = string.Format("update {0} set {1} {2}", def.objectName, string.Join(", ", columnsToUpdate), whereClause);

            log("Generated SQL (Update) " + sql);

            using (var connection = adapter.getConnection(ConnectionString))
            {

                adapter.openSQL(connection, sql);

                if (usePrimaryKeysInWhereClause)
                    adapter.addParams(connection, def.primaryKeys, values, "where_");
                else
                    adapter.addParams(connection, where, "where_");

                adapter.addParams(connection, columnsToUpdateParams, values, "update_");

                return connection.command.ExecuteNonQuery();

            }

        }

        private DataSet executeProcForDataSet(string procedureName, object paramObject, out int rowsFilled)
        {

            using (var connection = adapter.getConnection(ConnectionString))
            {

                adapter.openSProc(connection, procedureName);

                adapter.addSProcParams(connection, paramObject);

                DataSet ds;
                rowsFilled = adapter.fillDataSet(connection, out ds);

                return ds;

            }

        }

        private void log(string msg)
        {

            if (loggerMethod != null)
                loggerMethod(msg);

        }

        private void checkWhereForMultiTenantID(TableDefinition def, object where)
        {

            //check for multi tenant columns in where clause
            if (def.multiTenantIDColumns.Count > 0)
            {

                if (where == null)
                    throw new Exception("Where clause cannot be ommitted when model has multi tenant identifiers.");

                multiTenantMatch(def, where.GetType().GetProperties());

            }

        }

        private void multiTenantMatch(TableDefinition def, PropertyInfo[] props)
        {

            foreach (PropertyInfo i in def.multiTenantIDColumns)
            {

                bool matched = false;
                foreach (PropertyInfo w in props)
                    if (w.Name == i.Name)
                    {
                        matched = true;
                        break;
                    }

                if (!matched)
                    throw new Exception(string.Format("Where clause does not contain multi tenant id ({0}).", i.Name));

            }

        }

        private void select(Type type, int top, object where, bool useOrLogic, out DataSet ds, out int rowsFilled)
        {

            TableDefinition def = getDef(type);

            checkWhereForMultiTenantID(def, where);

            //generate columns
            List<string> columnsToSelect = new List<string>();
            foreach (PropertyInfo prop in def.allColumns)
                if (!Utilities.HasAttribute<IgnoreOnSelect>(prop))
                    columnsToSelect.Add(string.Format("{0}.{1}", def.objectName, adapter.getObjectName(Utilities.GetColumnName(prop))));

            //generate where clause
            string whereClause = "";
            bool addParams = true;
            if (where != null)
                if (where is string)
                {
                    whereClause = "where " + where;
                    addParams = false;
                }
                else
                    whereClause = "where " + generateWhereClause(def.tableName, where, useOrLogic, "where_");

            //generate join
            string joinClause = "";
            if (def.linkedColumns.Count > 0)
            {

                List<string> joinClauseList = new List<string>();

                int joinIndex = 0;
                foreach (PropertyInfo linkedField in def.linkedColumns)
                {

                    ForeignColumn annotation = linkedField.GetCustomAttribute<ForeignColumn>(false);

                    if (annotation.objectModel != null)
                        annotation.tableName = Utilities.GetTableName(annotation.objectModel);

                    string joinPart = "join";
                    if (annotation.joinType == JoinType.LEFT)
                        joinPart = "left join";
                    else if (annotation.joinType == JoinType.RIGHT)
                        joinPart = "right join";
                    else if (annotation.joinType == JoinType.FULL_OUTER)
                        joinPart = "full outer join";

                    string correlateTable = adapter.getObjectName(annotation.tableName + "_" + joinIndex);

                    joinClauseList.Add(string.Format("{4} {0} {5} on {5}.{1} = {2}.{3} ",
                        adapter.getObjectName(annotation.tableName),
                        adapter.getObjectName(annotation.PrimaryKey),
                        def.objectName,
                        adapter.getObjectName(annotation.ForeignKey),
                        joinPart,
                        correlateTable));

                    columnsToSelect.Add(string.Format("{0}.{1} as {2}",
                        correlateTable,
                        adapter.getObjectName(Utilities.GetColumnName(linkedField)),
                        adapter.getObjectName(linkedField.Name)));

                    joinIndex++;

                }

                joinClause = string.Join("\r\n", joinClauseList.Distinct());

            }

            //generate select statement
            string sql = adapter.selectSQL(def.tableName, columnsToSelect, joinClause, whereClause, top);

            log("Generated SQL (select) " + sql);

            using (var connection = adapter.getConnection(ConnectionString))
            {

                //set sql
                connection.command.CommandText = sql;

                //add parameters
                if (addParams)
                    adapter.addParams(connection, where, "where_");

                //fill data set
                rowsFilled = adapter.fillDataSet(connection, out ds);

            }

        }

        private string generateWhereClause(string tableName, object whereClause, bool orLogic, string paramPrefix)
        {

            if (whereClause is string)
                return adapter.parseWhereString((string)whereClause, paramPrefix);

            List<PropertyInfo> props = new List<PropertyInfo>(whereClause.GetType().GetProperties());
            return generateWhereClause(tableName, props, whereClause, orLogic, paramPrefix);

        }

        private string generateWhereClause(string tableName, List<PropertyInfo> properties, object valueObject, bool orLogic, string paramPrefix)
        {

            List<string> conditions = new List<string>();
            foreach (PropertyInfo property in properties)
                conditions.Add(generateAssignmentClause(tableName, property, valueObject, paramPrefix, true));

            return string.Join(orLogic ? " or " : " and ", conditions);

        }

        private string generateAssignmentClause(string tableName, PropertyInfo property, object valueObject, string paramPrefix, bool isCondition)
        {
            object value = property.GetValue(valueObject);
            string columnName = Utilities.GetColumnName(property);
            string param = adapter.getParamName(paramPrefix + columnName);
            if (isCondition)
                return string.Format(value == null || value == DBNull.Value ? "{0}.{1} is null" : "{0}.{1} = {2}",
                    adapter.getObjectName(tableName),
                    adapter.getObjectName(columnName),
                    param);
            else
                return string.Format("{0}.{1} = {2}",
                    adapter.getObjectName(tableName),
                    adapter.getObjectName(Utilities.GetColumnName(property)),
                    param);
        }

        internal TableDefinition getDef(Type type)
        {

            if (!Utilities.HasAttribute<Table>(type))
                throw new Exception(string.Format("Cannot define object {0}.  Did you forget your Table annotation?", type.Name));

            if (!tableDefinitions.ContainsKey(type))
            {

                //create new definition
                TableDefinition def = new TableDefinition(this);

                //get object properties
                PropertyInfo[] properties = type.GetProperties();

                foreach (PropertyInfo property in properties)
                {

                    //add links to foreign keys
                    if (Utilities.HasAttribute<ForeignColumn>(property))
                    {

                        def.linkedColumns.Add(property);
                        continue;

                    }

                    //ignore column
                    if (!Utilities.HasAttribute<Column>(property))
                        continue;

                    //get column
                    Column column = property.GetCustomAttribute<Column>(false);

                    //add primary key
                    if (Utilities.HasAttribute<PrimaryKey>(property))
                        def.primaryKeys.Add(property);

                    //multi tenant columns
                    if (column.IsMultiTenantID)
                        def.multiTenantIDColumns.Add(property);

                    //add too all fields
                    def.allColumns.Add(property);

                }

                //check to make sure linked foreign keys exist
                //foreach (PropertyInfo linkedField in def.linkedColumns)
                //{
                //    ForeignColumn annotation = linkedField.GetCustomAttribute<ForeignColumn>(false);
                //    if (def.getForeginKey(annotation.ForeignKey) == null)
                //        throw new Exception(string.Format("Foreign key {0} does not exist in class {1}.  Forgot ForeignKey attribute?", annotation.ForeignKey, type.Name));
                //}

                //get table name
                def.tableName = Utilities.GetTableName(type);
                def.objectName = adapter.getObjectName(def.tableName);

                if (string.IsNullOrEmpty(def.tableName))
                    throw new Exception(string.Format("Could not define object {0}.  Could not determine table name.", type.Name));

                //get fields
                FieldInfo[] fields = type.GetFields();
                foreach (FieldInfo field in fields)
                {

                    //ignore column
                    if (!Utilities.HasAttribute<Column>(field) || !field.IsStatic)
                        continue;

                    def.fields.Add(field);

                }


                tableDefinitions[type] = def;

                log(string.Format("Defined object: {0} - {1}", def.tableName, string.Join(", ", def.allColumns)));

            }

            return tableDefinitions[type];

        }

        #endregion

    }

}
