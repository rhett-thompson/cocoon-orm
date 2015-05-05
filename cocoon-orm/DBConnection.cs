
using Cocoon.Annotations;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace Cocoon
{

    /// <summary>
    /// The database connection class
    /// </summary>
    public class DBConnection
    {

        /// <summary>
        /// The connection string EspressoDB is using
        /// </summary>
        public readonly string ConnectionString;

        private Dictionary<Type, ObjectDefinition> objectDefinitions = new Dictionary<Type, ObjectDefinition>();
        private Action<string> logMethod;
        private DBAdapterTarget target;

        #region Public Interface

        /// <summary>
        /// Creates a new database connection
        /// </summary>
        /// <param name="ConnectionString">The connection string to use to connect to the database</param>
        /// <param name="LogMethod">A method to call for logging purposes</param>
        public DBConnection(string ConnectionString, Action<string> LogMethod = null)
        {

            this.ConnectionString = ConnectionString;
            this.logMethod = LogMethod;
            this.target = DBAdapterTarget.SQLServer;

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

            //fill list
            List<T> list = new List<T>();
            foreach (DataRow row in ds.Tables[0].Rows)
            {

                if (hasAttribute<Table>(type) && fieldToMap == null)
                {

                    T item = (T)Activator.CreateInstance(type);
                    setFromRow<T>(item, row);
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

            using (var adapter = new DBAdapter(target, ConnectionString))
            {

                adapter.openSProc(procedureName);

                adapter.addSProcParams(paramObject);

                return adapter.command.ExecuteNonQuery();

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

            using (var adapter = new DBAdapter(target, ConnectionString))
            {

                adapter.openSProc(procedureName);

                adapter.addSProcParams(paramObject);

                return (T)adapter.command.ExecuteScalar();

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

                if (hasAttribute<Table>(type))
                {

                    T item = (T)Activator.CreateInstance(type);
                    setFromRow<T>(item, row);
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

            using (var adapter = new DBAdapter(target, ConnectionString))
            {

                adapter.openSQL(sql);

                adapter.addParams(paramObject, "");

                DataSet ds;
                int rowsFilled = adapter.fillDataSet(out ds);

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

            using (var adapter = new DBAdapter(target, ConnectionString))
            {

                adapter.openSQL(sql);

                adapter.addParams(paramObject, "");

                return adapter.command.ExecuteNonQuery();

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

            using (var adapter = new DBAdapter(target, ConnectionString))
            {

                adapter.openSQL(sql);

                adapter.addParams(paramObject, "");

                return (T)adapter.command.ExecuteScalar();

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
            setFromRow<T>(obj, ds.Tables[0].Rows[0]);

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

            //get type
            Type type = typeof(T);

            //do select
            DataSet ds;
            int rowsFilled;
            select(type, top, where, useOrLogic, out ds, out rowsFilled);

            if (rowsFilled == 0)
                return new List<T>();

            //fill list
            List<T> list = new List<T>();
            foreach (DataRow row in ds.Tables[0].Rows)
            {
                T obj = (T)Activator.CreateInstance(type);
                setFromRow<T>(obj, row);
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

            using (var adapter = new DBAdapter(target, ConnectionString))
            {

                adapter.command.CommandText = sql;

                adapter.addParams(where, "where_");

                adapter.connection.Open();
                return (T)adapter.command.ExecuteScalar();
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

            ObjectDefinition def = getDef(objectModel);

            return GetScalar<T>(def.TableName, scalarField, where, useOrLogic);

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

            using (var adapter = new DBAdapter(target, ConnectionString))
            {

                adapter.command.CommandText = sql;

                adapter.addParams(where, "where_");

                //fill data set
                DataSet ds;
                int rowsFilled = adapter.fillDataSet(out ds);

                if (rowsFilled == 0)
                    return new List<T>();

                //fill list
                List<T> list = new List<T>();
                foreach (DataRow row in ds.Tables[0].Rows)
                {
                    if (fieldToMap == null)
                        list.Add((T)row[0]);
                    else if (row.Table.Columns.Contains(fieldToMap))
                        list.Add((T)row[fieldToMap]);
                }

                return list;

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
            ObjectDefinition def = getDef(objectModel);

            return GetScalarList<T>(def.TableName, where, fieldToMap, useOrLogic, top);

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

            string whereClause = generateWhereClause(tableName, where, useOrLogic, "where_");

            string sql = string.Format("delete from {0} where {1}", tableName, whereClause);

            log("Generated SQL (Delete) " + sql);

            using (var adapter = new DBAdapter(target, ConnectionString))
            {

                //set sql
                adapter.command.CommandText = sql;

                //add parameters
                adapter.addParams(where, "where_");

                //execute
                adapter.connection.Open();

                return adapter.command.ExecuteNonQuery();

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

            return Delete(getDef(objectModel).TableName, where, useOrLogic);

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
            ObjectDefinition def = getDef(objectToUpdate.GetType());

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
        /// Inserts a new record into the database and returns an output object.
        /// </summary>
        /// <typeparam name="T">The type of object to return</typeparam>
        /// <param name="objectToInsert">The object to use for the insert</param>
        /// <returns></returns>
        public T Insert<T>(object objectToInsert)
        {

            ObjectDefinition def = getDef(objectToInsert.GetType());

            Type returnType = typeof(T);

            //get columns and values
            List<string> columns = new List<string>();
            List<string> values = new List<string>();
            List<string> insertedPrimaryKeys = new List<string>();
            List<string> primaryKeys = new List<string>();
            List<string> wherePrimaryKeys = new List<string>();
            List<PropertyInfo> parameters = new List<PropertyInfo>();
            foreach (PropertyInfo prop in def.allColumns)
            {
                if (!hasAttribute<IgnoreOnInsert>(prop))
                {
                    string propName = getColumnName(prop);
                    columns.Add(string.Format("{0}.{1}", def.TableName, propName));
                    values.Add(string.Format("@value_{0}", propName));
                    parameters.Add(prop);
                }

                if (hasAttribute<PrimaryKey>(prop))
                {
                    string propName = getColumnName(prop);
                    insertedPrimaryKeys.Add("inserted." + propName);
                    primaryKeys.Add(string.Format("{0} {1}", propName, TypeMap.sqlMap[prop.PropertyType]));
                    wherePrimaryKeys.Add(string.Format("ids.{0} = {1}.{0}", propName, def.TableName));
                }

            }

            //put together sql
            string sql = string.Format("declare @ids table({0});insert into {1} ({2}) output {3} into @ids values ({4});select {1}.* from {1} join @ids ids on {5}",
                string.Join(", ", primaryKeys),
                def.TableName,
                string.Join(", ", columns),
                string.Join(", ", insertedPrimaryKeys),
                string.Join(", ", values),
                string.Join(" and ", wherePrimaryKeys));

            log("Generated SQL (Insert) " + sql);

            using (var adapter = new DBAdapter(target, ConnectionString))
            {

                //set sql
                adapter.openSQL(sql);

                //add parameters
                adapter.addParams(parameters, objectToInsert, "value_");

                //get return
                if (hasAttribute<Table>(returnType))
                {

                    DataSet ds;
                    int rowsFilled = adapter.fillDataSet(out ds);

                    if (rowsFilled == 0)
                        return default(T);

                    T objectToReturn = (T)Activator.CreateInstance(returnType);
                    setFromRow<T>(objectToReturn, ds.Tables[0].Rows[0]);

                    return objectToReturn;

                }
                else
                {

                    return (T)adapter.command.ExecuteScalar();

                }

            }

        }

        #endregion

        #region Utility API

        /// <summary>
        /// 
        /// </summary>
        /// <param name="objectModel"></param>
        /// <returns></returns>
        public bool TableExists(Type objectModel)
        {

            ObjectDefinition def = getDef(objectModel);

            string sql = string.Format("if not exists (select name from sysobjects where name = '{0}') select cast(0 as bit) else select cast(1 as bit)", removeNameBrackets(def.TableName));

            log("Generated SQL (TableExists) " + sql);

            return ExecuteSQLScalar<bool>(sql);

        }

        /// <summary>
        /// Creates a table based on objectModel.  The database is only modified if the table does not already exist.
        /// </summary>
        /// <param name="objectModel"></param>
        public void CreateTable(Type objectModel)
        {

            ObjectDefinition def = getDef(objectModel);

            List<string> columns = new List<string>();
            List<string> primaryKeys = new List<string>();
            foreach (PropertyInfo prop in def.allColumns)
            {
                columns.Add(getColumnDefinition(prop));

                if (hasAttribute<PrimaryKey>(prop))
                    primaryKeys.Add(getColumnName(prop));

            }

            if (primaryKeys.Count > 0)
                columns.Add(string.Format("primary key ({0})", string.Join(", ", primaryKeys)));

            string sql = string.Format("if not exists (select name from sysobjects where name = '{0}') create table {1} ({2})",
                removeNameBrackets(def.TableName),
                def.TableName,
                string.Join(", ", columns));

            log("Generated SQL (CreateTable) " + sql);

            ExecuteSQLVoid(sql);

        }

        /// <summary>
        /// Creates a lookup type table based on objectModel.  The database is only modified if the table does not already exist.
        /// Only static fields will be used; properties will be ignored.
        /// </summary>
        /// <param name="objectModel"></param>
        public void CreateLookupTable(Type objectModel)
        {

            string tableName = getTableName(objectModel);

            FieldInfo[] fields = objectModel.GetFields();
            List<string> columns = new List<string>();
            List<string> columnNames = new List<string>();
            List<KeyValuePair<string, object>> values = new List<KeyValuePair<string, object>>();
            List<string> primaryKeys = new List<string>();
            foreach (FieldInfo field in fields)
            {

                if (!field.IsStatic || !hasAttribute<Column>(field))
                    continue;

                string columnName = getColumnName(field);

                if (!columnNames.Contains(columnName))
                {
                    columnNames.Add(columnName);
                    columns.Add(getColumnDefinition(field));
                }

                values.Add(new KeyValuePair<string, object>(columnName, field.GetValue(null)));

                if (hasAttribute<PrimaryKey>(field))
                    primaryKeys.Add(columnName);

            }

            if (columns.Count == 0)
                throw new Exception("No columns for table " + objectModel.Name);

            if (values.Count == 0)
                throw new Exception("No values to insert for table " + objectModel.Name);

            if (primaryKeys.Count > 0)
                columns.Add(string.Format("primary key ({0})", string.Join(", ", primaryKeys)));

            string insert = "";
            foreach (KeyValuePair<string, object> value in values)
                insert += string.Format("insert into {0} ({1}) values ('{2}') ", tableName, value.Key, value.Value);

            string sql = string.Format("if not exists (select name from sysobjects where name = '{0}') begin create table {1} ({2}) {3} end", removeNameBrackets(tableName), tableName, string.Join(", ", columns), insert);

            log("Generated SQL (CreateLookupTable) " + sql);

            ExecuteSQLVoid(sql);

        }

        /// <summary>
        /// Drops a table from the database
        /// </summary>
        /// <param name="objectModel"></param>
        public void DropTable(Type objectModel)
        {

            ObjectDefinition def = getDef(objectModel);

            DropTable(def.TableName);

        }

        /// <summary>
        /// Drops a table from the database
        /// </summary>
        /// <param name="TableName"></param>
        public void DropTable(string TableName)
        {

            string sql;

            if(target == DBAdapterTarget.MySQL)
                sql = string.Format("drop table if exists `{0}`", TableName);
            else
                sql = string.Format("if exists (select name from sysobjects where name = '{0}') drop table {1}", removeNameBrackets(TableName), TableName);

            log("Generated SQL (DropTable) " + sql);

            ExecuteSQLVoid(sql);

        }

        /// <summary>
        /// Verifies an object model matches a table
        /// </summary>
        /// <param name="objectModel"></param>
        /// <returns></returns>
        public bool VerifyTable(Type objectModel)
        {

            ObjectDefinition def = getDef(objectModel);

            DataSet ds = ExecuteSQLDataSet(string.Format("select COLUMN_NAME from INFORMATION_SCHEMA.COLUMNS where TABLE_NAME='{0}'", removeNameBrackets(def.TableName)));

            if (ds == null || ds.Tables.Count != 1 || def.allColumns.Count != ds.Tables[0].Rows.Count)
                return false;

            foreach (DataRow row in ds.Tables[0].Rows)
                if (!def.hasColumn((string)row["COLUMN_NAME"]))
                    return false;

            return true;

        }

        /// <summary>
        /// Verifies the static class values match a look up table
        /// </summary>
        /// <param name="objectModel"></param>
        /// <returns></returns>
        public bool VerifyLookupTable(Type objectModel)
        {

            DataSet ds = ExecuteSQLDataSet(string.Format("select * from {0}", getTableName(objectModel)));

            if (ds == null || ds.Tables.Count != 1 || ds.Tables[0].Rows.Count == 0 || ds.Tables[0].Columns.Count == 0)
                return false;

            //get valid fields
            FieldInfo[] fields = objectModel.GetFields();
            List<FieldInfo> validFields = new List<FieldInfo>();
            foreach (FieldInfo field in fields)
                if (field.IsStatic)
                {

                    if (!ds.Tables[0].Columns.Contains(getColumnName(field)))
                        return false;

                    validFields.Add(field);

                }

            if (ds.Tables[0].Rows.Count != validFields.Count)
                return false;

            //check values
            foreach (FieldInfo field in validFields)
                if (!lookupTableColumnHasValue(ds.Tables[0].Rows, getColumnName(field), field.GetValue(null)))
                    return false;

            return true;

        }

        /// <summary>
        /// Generates a C# class from a database table schema.
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public string GenerateClassFromTable(string tableName)
        {

            string sql = @"
                select 
	                columnTable.COLUMN_NAME, 
	                columnTable.COLUMN_DEFAULT, 
	                columnTable.IS_NULLABLE, 
	                columnTable.DATA_TYPE, 
	                columnTable.CHARACTER_MAXIMUM_LENGTH,
	                contraintTable.CONSTRAINT_TYPE
                from INFORMATION_SCHEMA.COLUMNS as columnTable
                left join INFORMATION_SCHEMA.KEY_COLUMN_USAGE as keyTable on keyTable.COLUMN_NAME = columnTable.COLUMN_NAME and keyTable.TABLE_NAME = columnTable.TABLE_NAME
                left join INFORMATION_SCHEMA.TABLE_CONSTRAINTS as contraintTable on contraintTable.CONSTRAINT_NAME = keyTable.CONSTRAINT_NAME and contraintTable.TABLE_NAME = columnTable.TABLE_NAME
                where columnTable.TABLE_NAME='{0}'";

            DataSet ds = ExecuteSQLDataSet(string.Format(sql, removeNameBrackets(tableName)));

            if (ds == null || ds.Tables.Count != 1 || ds.Tables[0].Rows.Count == 0)
                throw new Exception(string.Format("No Columns in table {0}.", tableName));

            List<string> members = new List<string>();
            foreach (DataRow row in ds.Tables[0].Rows)
            {

                string columnName = (string)row["COLUMN_NAME"];
                string columnDefault = row["COLUMN_DEFAULT"] != DBNull.Value ? (string)row["COLUMN_DEFAULT"] : null;
                bool columnIsNullable = (string)row["IS_NULLABLE"] == "NO" ? false : true;
                string columnDataType = (string)row["DATA_TYPE"];
                string columnKeyType = row["CONSTRAINT_TYPE"] != DBNull.Value ? (string)row["CONSTRAINT_TYPE"] : null;

                List<string> annotations = new List<string>();

                if (!string.IsNullOrEmpty(columnDefault))
                    annotations.Add(string.Format("Column(DefaultValue:\"{0}\")", columnDefault));
                else
                    annotations.Add(string.Format("Column"));

                if (columnKeyType == "PRIMARY KEY")
                    annotations.Add("PrimaryKey");
                else if (columnKeyType == "FOREIGN KEY")
                    annotations.Add("ForeignKey");

                if (!columnIsNullable)
                    annotations.Add("NotNull");

                string dataType = "string";
                Type type = typeof(string);
                if (TypeMap.csMap.ContainsKey(columnDataType))
                {
                    type = TypeMap.csMap[columnDataType];
                    if (TypeMap.csAlias.ContainsKey(type))
                        dataType = TypeMap.csAlias[type];
                    else
                        dataType = type.Name;

                }

                if (columnIsNullable && type.IsValueType)
                    dataType += "?";

                members.Add(string.Format("\t[{0}]\r\n\tpublic {1} {2} {{get; set;}}", string.Join(", ", annotations), dataType, row["COLUMN_NAME"]));

            }

            return string.Format("[Table]\r\nclass {0}\r\n{{\r\n\r\n{1}\r\n\r\n}}", tableName, string.Join("\r\n\r\n", members));

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

        private int update(ObjectDefinition def, List<PropertyInfo> fieldsToUpdate, object values, object where = null, bool useOrLogic = false)
        {

            //generate where clause
            string whereClause = "";
            bool usePrimaryKeysInWhereClause = false;
            if (where != null)
                whereClause = "where " + generateWhereClause(def.TableName, where, useOrLogic, "where_");
            else if (def.primaryKeys.Count > 0)
            {
                usePrimaryKeysInWhereClause = true;
                whereClause = "where " + generateWhereClause(def.TableName, def.primaryKeys, values, useOrLogic, "where_");
            }

            //generate columns
            List<string> columnsToUpdate = new List<string>();
            List<PropertyInfo> columnsToUpdateParams = new List<PropertyInfo>();
            foreach (PropertyInfo field in fieldsToUpdate)
                if (!hasAttribute<IgnoreOnUpdate>(field))
                {
                    columnsToUpdate.Add(generateAssignmentClause(def.TableName, field, values, "update_", false));
                    columnsToUpdateParams.Add(field);
                }

            string sql = string.Format("update {0} set {1} {2}", def.TableName, string.Join(", ", columnsToUpdate), whereClause);

            log("Generated SQL (Update) " + sql);

            using (var adapter = new DBAdapter(target, ConnectionString))
            {

                adapter.openSQL(sql);

                if (usePrimaryKeysInWhereClause)
                    adapter.addParams(def.primaryKeys, values, "where_");
                else
                    adapter.addParams(where, "where_");

                adapter.addParams(columnsToUpdateParams, values, "update_");

                return adapter.command.ExecuteNonQuery();

            }

        }

        private string removeNameBrackets(string name)
        {

            return name.Replace("[", "").Replace("]", "");

        }

        private bool lookupTableColumnHasValue(DataRowCollection rows, string columnName, object value)
        {

            foreach (DataRow row in rows)
            {
                object r = row[columnName];
                if (r.Equals(value))
                    return true;
            }

            return false;

        }

        private string getColumnDefinition(MemberInfo member)
        {

            Column columnAnnotation = member.GetCustomAttribute<Column>(false);
            string columnName = getColumnName(member);

            //data type
            string dataType = "";
            if (!string.IsNullOrEmpty(columnAnnotation.dataType))
                dataType = columnAnnotation.dataType;
            else if (member.MemberType == MemberTypes.Field && TypeMap.sqlMap.ContainsKey(((FieldInfo)member).FieldType))
                dataType = TypeMap.sqlMap[((FieldInfo)member).FieldType];
            else if (member.MemberType == MemberTypes.Property && TypeMap.sqlMap.ContainsKey(((PropertyInfo)member).PropertyType))
                dataType = TypeMap.sqlMap[((PropertyInfo)member).PropertyType];
            else
                throw new Exception(string.Format("Could not determine data type for column {0} for table {1}.", member.Name, member.ReflectedType.Name));

            //not null
            string notNull = "";
            if (hasAttribute<NotNull>(member))
                notNull = "not null";

            //default value
            string defaultValue = "";
            if (hasAttribute<Identity>(member))
            {
                Identity identityAnnotation = member.GetCustomAttribute<Identity>(false);
                defaultValue = string.Format("identity({0}, {1})", identityAnnotation.seed, identityAnnotation.increment);
            }
            else if (!string.IsNullOrEmpty(columnAnnotation.defaultValue))
                defaultValue = string.Format("default {0}", columnAnnotation.defaultValue);

            //foreign key
            string foreignKey = "";
            if (hasAttribute<ForeignKey>(member))
            {

                ForeignKey foreignKeyAnnotation = member.GetCustomAttribute<ForeignKey>(false);

                if (foreignKeyAnnotation.referencesTable != null)
                {

                    string primaryKeyColumn = columnName;
                    if (!string.IsNullOrEmpty(foreignKeyAnnotation.referenceTablePrimaryKeyOverride))
                        primaryKeyColumn = foreignKeyAnnotation.referenceTablePrimaryKeyOverride;

                    foreignKey = string.Format("foreign key references {0}({1})", getTableName(foreignKeyAnnotation.referencesTable), primaryKeyColumn);
                }

            }

            //generate column
            string column = string.Format("{0} {1} {2} {3} {4}", columnName, dataType, notNull, defaultValue, foreignKey);
            return Regex.Replace(column, @"\s+", " ");

        }

        private DataSet executeProcForDataSet(string procedureName, object paramObject, out int rowsFilled)
        {

            using (var adapter = new DBAdapter(target, ConnectionString))
            {

                adapter.openSProc(procedureName);

                adapter.addSProcParams(paramObject);

                DataSet ds;
                rowsFilled = adapter.fillDataSet(out ds);

                return ds;

            }

        }

        private void log(string msg)
        {

            if (logMethod != null)
                logMethod(msg);

        }

        private void select(Type type, int top, object where, bool useOrLogic, out DataSet ds, out int rowsFilled)
        {

            ObjectDefinition def = getDef(type);

            //generate columns
            List<string> columnsToSelect = new List<string>();
            foreach (PropertyInfo prop in def.allColumns)
                if (!hasAttribute<IgnoreOnSelect>(prop))
                    columnsToSelect.Add(string.Format("{0}.{1}", def.TableName, getColumnName(prop)));

            //generate where clause
            string whereClause = "";
            if (where != null)
                whereClause = "where " + generateWhereClause(def.TableName, where, useOrLogic, "where_");

            string topClause = "";
            if (top > 0)
                topClause = string.Format("top {0}", top);

            //generate join
            string joinClause = "";
            if (def.linkedColumns.Count > 0)
            {

                List<string> joinClauseList = new List<string>();

                foreach (PropertyInfo linkedField in def.linkedColumns)
                {

                    ForeignColumn annotation = linkedField.GetCustomAttribute<ForeignColumn>(false);

                    joinClauseList.Add(string.Format("join {0} on {0}.{1} = {2}.{3} ", annotation.tableName, annotation.primaryKey, def.TableName, annotation.foreignKey));
                    columnsToSelect.Add(string.Format("{0}.{1}", annotation.tableName, getColumnName(linkedField)));

                }

                joinClause = string.Join("\r\n", joinClauseList.Distinct());

            }

            //generate select statement
            string sql = string.Format("select {0} {1} from {2} {3} {4}",
                topClause,
                string.Join(", ", columnsToSelect),
                def.TableName,
                joinClause,
                whereClause);

            log("Generated SQL (select) " + sql);

            using (var adapter = new DBAdapter(target, ConnectionString))
            {

                //set sql
                adapter.command.CommandText = sql;

                //add parameters
                adapter.addParams(where, "where_");

                //fill data set
                rowsFilled = adapter.fillDataSet(out ds);

            }

        }

        private void setFromRow<T>(T objectToSet, DataRow row)
        {

            Type type = typeof(T);

            PropertyInfo[] propertiesToSet = type.GetProperties();
            foreach (PropertyInfo prop in propertiesToSet)
            {

                string propName = getColumnName(prop);

                if (!row.Table.Columns.Contains(propName))
                    continue;

                DataColumn column = row.Table.Columns[propName];

                if (column == null)
                    continue;

                try
                {

                    if (row[column] == DBNull.Value)
                        prop.SetValue(objectToSet, null);
                    else
                        prop.SetValue(objectToSet, row[column]);

                }
                catch
                {

                    throw new Exception(string.Format("Could not assign value to '{0}'.", propName));

                }

            }

        }

        private string generateWhereClause(string tableName, object whereClause, bool orLogic, string paramPrefix)
        {

            if (whereClause is string)
                return ((string)whereClause).Replace("@", "@" + paramPrefix);

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
            if (isCondition)
                return string.Format(value == null ? "{0}.{1} is null" : "{0}.{1} = @{2}{1}", tableName, getColumnName(property), paramPrefix);
            else
                return string.Format("{0}.{1} = @{2}{1}", tableName, getColumnName(property), paramPrefix);
        }

        private ObjectDefinition getDef(Type type)
        {

            if (!hasAttribute<Table>(type))
                throw new Exception(string.Format("Cannot define object {0}.  Did you forget your Table annotation?", type.Name));

            if (!objectDefinitions.ContainsKey(type))
            {

                //create new definition
                ObjectDefinition def = new ObjectDefinition();

                //get object properties
                PropertyInfo[] properties = type.GetProperties();

                foreach (PropertyInfo property in properties)
                {

                    //add links to foreign keys
                    if (hasAttribute<ForeignColumn>(property))
                    {

                        def.linkedColumns.Add(property);
                        continue;

                    }

                    //ignore column
                    if (!hasAttribute<Column>(property))
                        continue;

                    //add primary key
                    if (hasAttribute<PrimaryKey>(property))
                        def.primaryKeys.Add(property);

                    //add foreign key
                    if (hasAttribute<ForeignKey>(property))
                        def.foreignKeys.Add(property);

                    //add too all fields
                    def.allColumns.Add(property);

                }

                //check to make sure linked foreign keys exist
                foreach (PropertyInfo linkedField in def.linkedColumns)
                {
                    ForeignColumn annotation = linkedField.GetCustomAttribute<ForeignColumn>(false);
                    if (def.getForeginKey(annotation.foreignKey) == null)
                        throw new Exception(string.Format("Foreign key {0} does not exist in class {1}.  Forgot ForeignKey attribute?", linkedField.Name, type.Name));
                }

                //get table name
                def.TableName = getTableName(type);

                if (string.IsNullOrEmpty(def.TableName))
                    throw new Exception(string.Format("Could not define object {0}.  Could not determine table name.", type.Name));

                //get fields
                FieldInfo[] fields = type.GetFields();
                foreach (FieldInfo field in fields)
                {

                    //ignore column
                    if (!hasAttribute<Column>(field) || !field.IsStatic)
                        continue;

                    def.fields.Add(field);

                }


                objectDefinitions[type] = def;

                log(string.Format("Defined object: {0} - {1}", def.TableName, string.Join(", ", def.allColumns)));

            }

            return objectDefinitions[type];

        }

        internal static bool hasAttribute<T>(MemberInfo member)
        {

            return member.GetCustomAttributes(typeof(T), false).Length > 0;

        }

        internal static bool hasAttribute<T>(Type property)
        {

            return property.GetCustomAttributes(typeof(T), false).Length > 0;

        }

        internal static string getColumnName(MemberInfo member)
        {

            string name = null;
            string overrideName = null;

            if (hasAttribute<ForeignColumn>(member))
            {

                ForeignColumn annotation = member.GetCustomAttribute<ForeignColumn>(false);
                overrideName = annotation.overrideName;

            }
            else
            {

                Column annotation = member.GetCustomAttribute<Column>(false);
                if (annotation != null)
                    overrideName = annotation.overrideName;

            }

            if (!string.IsNullOrEmpty(overrideName))
                name = overrideName;
            else
                name = member.Name;

            if (ReservedWords.words.Contains(name.ToUpper()) || name.Contains(" "))
                return "[" + name + "]";
            else
                return name;

        }

        internal static string getTableName(Type type)
        {

            string name;
            if (hasAttribute<Table>(type))
            {

                Table annotation = type.GetCustomAttribute<Table>(false);

                if (annotation.tableName == null)
                    name = type.Name;
                else
                    name = annotation.tableName;

            }
            else
                name = type.Name;

            if (ReservedWords.words.Contains(name.ToUpper()) || name.Contains(" "))
                return "[" + name + "]";
            else
                return name;

        }

        #endregion

    }

}
