using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Net.NetworkInformation;
using System.Numerics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace Cocoon.ORM
{
    public class CocoonORM
    {

        public string ConnectionString;
        public int CommandTimeout = 15;

        internal Dictionary<Type, TableDefinition> tables = new Dictionary<Type, TableDefinition>();

        public CocoonORM(string connectionString)
        {

            ConnectionString = connectionString;

        }

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
        
        #region Basic CRUD

        public IEnumerable<T> GetList<T>(Expression<Func<T, bool>> where = null, int top = 0)
        {
            
            return GetList(typeof(T), where, top).Cast<T>();

        }

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

        public int Delete<T>(Expression<Func<T, bool>> where, int timeout = -1)
        {
            TableDefinition def = getTable(typeof(T));

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandTimeout = timeout < 0 ? CommandTimeout : timeout;
                string whereClause = generateWhereClause(cmd, def.objectName, where);

                cmd.CommandText = string.Format("delete from {0} {1}", def.objectName, whereClause);

                conn.Open();
                return cmd.ExecuteNonQuery();

            }
        }

        public int Update<T>(T objectToUpdate, Expression<Func<T, bool>> where = null, int timeout = -1)
        {

            if (objectToUpdate == null)
                throw new NullReferenceException("objectToUpdate cannot be null.");

            TableDefinition def = getTable(typeof(T));

            return update(def, objectToUpdate, def.columns, timeout, where);


        }

        public int Update<T>(Type model, object objectToUpdate, Expression<Func<T, bool>> where = null, int timeout = -1)
        {

            if (objectToUpdate == null)
                throw new NullReferenceException("objectToUpdate cannot be null.");

            TableDefinition def = getTable(model);

            return update(def, objectToUpdate, def.columns, timeout, where);


        }

        public int Update<T>(object fieldsToUpdate, Expression<Func<T, bool>> where = null, int timeout = -1)
        {

            if (fieldsToUpdate == null)
                throw new NullReferenceException("fieldsToUpdate cannot be null.");

            TableDefinition def = getTable(typeof(T));

            return update(def, fieldsToUpdate, fieldsToUpdate.GetType().GetProperties(), timeout, where);

        }

        public T Insert<T>(T objectToInsert, int timeout = -1)
        {

            TableDefinition def = getTable(typeof(T));

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

                        SqlParameter param = addParam(cmd, "insert_value_" + getGuid(), prop.GetValue(objectToInsert));
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
                    wherePrimaryKeys.Add(string.Format("ids.{0} = {1}.{0}", primaryKeyName, def.objectName));
                }

                //generate query
                cmd.CommandText = string.Format("{0};insert into {1} ({2}) output {3} into @ids values ({4});select {1}.* from {1} join @ids ids on {5}",
                    string.Format("declare @ids table({0})", string.Join(", ", outputTableKeys)),
                    def.objectName,
                    string.Join(", ", columns),
                    string.Join(", ", insertedPrimaryKeys),
                    string.Join(", ", values),
                    string.Join(" and ", wherePrimaryKeys));

                conn.Open();
                return readSingle<T>(cmd);

            }

        }

        public IEnumerable<T> Insert<T>(IEnumerable<T> objectsToInsert)
        {

            List<T> list = new List<T>();

            foreach (T obj in objectsToInsert)
                list.Add(Insert(obj));

            return list;

        }

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
                cmd.CommandText = string.Format("select count(*) from {0} {1}", def.objectName, whereClause);

                //execute sql
                conn.Open();
                return readScalar<int>(cmd);

            }

        }

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
                cmd.CommandText = string.Format("select checksum_agg(binary_checksum(*)) from {0} {1}", def.objectName, whereClause);

                //execute sql
                conn.Open();
                return readScalar<int>(cmd);

            }

        }

        public bool Exists<T>(Expression<Func<T, bool>> where = null)
        {

            return Count<T>(where) > 0;

        }

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
                            values.Add(addParam(cmd, "override_value_" + getGuid(), overrideProp.GetValue(overrideValues)).ParameterName);
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
                    outputTableKeys.Add(string.Format("{0} {1}", primaryKeyName, dbTypeMap[pk.PropertyType]));
                    insertedPrimaryKeys.Add("inserted." + primaryKeyName);
                    wherePrimaryKeys.Add(string.Format("ids.{0} = {1}.{0}", primaryKeyName, def.objectName));
                }

                //build command
                cmd.CommandText = string.Format("{0};insert into {1} ({2}) output {3} into @ids select {4} from {1} {6};select {1}.* from {1} join @ids ids on {5}",
                    string.Format("declare @ids table({0})", string.Join(", ", outputTableKeys)),
                    def.objectName,
                    string.Join(", ", columns),
                    string.Join(", ", insertedPrimaryKeys),
                    string.Join(", ", values),
                    string.Join(" and ", wherePrimaryKeys),
                    whereClause);

                conn.Open();

                List<object> list = new List<object>();
                readList(cmd, def.type, list);

                return list.Cast<T>();

            }

        }

        #endregion

        #region SQL

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

        public static string GenerateSequentialUID()
        {

            return Base36Encode(DateTime.Now.Ticks);

        }

        public static long Base36Decode(string value)
        {

            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Empty value.");

            value = value.ToUpper();

            bool negative = false;

            if (value[0] == '-')
            {
                negative = true;
                value = value.Substring(1, value.Length - 1);
            }

            if (value.Any(c => !base36Digits.Contains(c)))
                throw new ArgumentException("Invalid value: \"" + value + "\".");

            long decoded = 0L;

            for (var i = 0; i < value.Length; ++i)
                decoded += base36Digits.IndexOf(value[i]) * (long)BigInteger.Pow(base36Digits.Length, value.Length - i - 1);

            return negative ? decoded * -1 : decoded;

        }

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

        public static bool HasAttribute<T>(MemberInfo member)
        {

            return member.GetCustomAttributes(typeof(T), false).Length > 0;

        }

        public static bool HasAttribute<T>(Type property)
        {

            return property.GetCustomAttributes(typeof(T), false).Length > 0;

        }

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

        public static IEnumerable<T> FillScalarList<T>(DataTable table, string fieldToMap = null)
        {

            return FillScalarList<T>(table.Select(), fieldToMap);

        }

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

        public static IEnumerable<object> FillList(Type type, DataTable table)
        {

            return FillList(type, table.Select());

        }

        public static IEnumerable<T> FillList<T>(IEnumerable<DataRow> rows)
        {

            return FillList(typeof(T), rows).Cast<T>().ToList();

        }

        public static IEnumerable<T> FillList<T>(DataTable table)
        {

            return FillList(typeof(T), table.Select()).Cast<T>().ToList();

        }

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

        public static string SHA256(string value)
        {
            using (SHA256 hash = System.Security.Cryptography.SHA256.Create())
            {
                return string.Join("", hash
                  .ComputeHash(Encoding.UTF8.GetBytes(value))
                  .Select(item => item.ToString("x2")));
            }

        }

        public static string MD5(string value)
        {

            using (MD5 hash = System.Security.Cryptography.MD5.Create())
            {
                return string.Join("", hash
                  .ComputeHash(Encoding.UTF8.GetBytes(value))
                  .Select(item => item.ToString("x2")));
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
            cmd.CommandText = string.Format("select {0} {1} from {2} {3} {4}", topClause, string.Join(", ", columnsToSelect), tableObjectName, joinClause, whereClause);

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
                        SqlParameter param = addParam(cmd, "update_field_" + getGuid(), prop.GetValue(values));
                        columnsToUpdate.Add(string.Format("{0}.{1} = {2}", def.objectName, getObjectName(prop), param.ParameterName));
                    }

                    if (HasAttribute<PrimaryKey>(prop) && where == null)
                    {
                        SqlParameter param = addParam(cmd, "where_" + getGuid(), prop.GetValue(values));
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

        internal static string getName(MemberInfo member)
        {

            string name = member.Name;
            if (HasAttribute<OverrideName>(member))
                name = member.GetCustomAttribute<OverrideName>().name;

            return name;

        }

        internal static string getObjectName(MemberInfo member)
        {

            return getObjectName(getName(member));

        }

        internal static string getObjectName(string name)
        {

            return string.Format("[{0}]", name);

        }

        internal static string getGuid()
        {

            return Guid.NewGuid().ToString("n");

        }

        internal static SqlParameter addParam(SqlCommand cmd, string name, object value)
        {

            SqlParameter param = cmd.CreateParameter();
            param.ParameterName = "@" + name;
            param.Value = value ?? Convert.DBNull;

            return cmd.Parameters.Add(param);

        }

        internal static string addWhereParam(SqlCommand cmd, object value)
        {

            return value == null ? "null" : addParam(cmd, string.Format("where_{0}", getGuid()), value).ParameterName;

        }

        internal string generateWhereClause(SqlCommand cmd, string tableObjectName, Expression where)
        {

            if (where == null)
                return null;

            SQLExpressionTranslator translater = new SQLExpressionTranslator();
            return "where " + translater.GenerateSQLExpression(this, cmd, where, tableObjectName);

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
                string alias = getObjectName(string.Format("join_table_{0}", getGuid()));
                string foreignField = getObjectName(foreignColumn);

                string joinPart = "join";
                if (attr.joinType == JoinType.LEFT)
                    joinPart = "left join";
                else if (attr.joinType == JoinType.RIGHT)
                    joinPart = "right join";
                else if (attr.joinType == JoinType.FULL_OUTER)
                    joinPart = "full outer join";

                joinClauseList.Add(string.Format("{0} {1} as {2} on {3}.{4} = {5}.{6}",
                    joinPart,
                    otherTableObjectName,
                    alias,
                    tableObjectName,
                    attr.KeyInThisTableModel,
                    alias,
                    attr.KeyInOtherTableModel
                ));

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
