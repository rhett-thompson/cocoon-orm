using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Cocoon.ORM
{
    /// <summary>
    /// 
    /// </summary>
    public class TransactSQLPlatform : SQLPlatform
    {

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override DbConnection getConnection()
        {
            return new SqlConnection(db.ConnectionString);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        public override DbDataAdapter getDataAdapter(DbCommand cmd)
        {

            return new SqlDataAdapter((SqlCommand)cmd);

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public override DbParameter addParam(DbCommand cmd, string name, object value)
        {
            if (value is DateTime && (DateTime)value < SqlDateTime.MinValue.Value)
                value = SqlDateTime.MinValue.Value;

            DbParameter param = cmd.CreateParameter();
            param.ParameterName = "@" + name;
            param.Value = value ?? Convert.DBNull;

            return cmd.Parameters[cmd.Parameters.Add(param)];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="parameters"></param>
        public override void addParamObject(DbCommand cmd, object parameters)
        {
            if (parameters != null)
                if (parameters is DbParameterCollection)
                    foreach (DbParameter p in (DbParameterCollection)parameters)
                        cmd.Parameters.Add(p);
                else
                    foreach (PropertyInfo prop in parameters.GetType().GetProperties())
                        addParam(cmd, prop.Name, prop.GetValue(parameters));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public override string addWhereParam(DbCommand cmd, object value)
        {
            return value == null ? "null" : addParam(cmd, $"where_{getGuidString(Guid.NewGuid())}", value).ParameterName;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tableObjectName"></param>
        /// <param name="columnsToSelect"></param>
        /// <param name="joins"></param>
        /// <returns></returns>
        public override string generateJoinClause(string tableObjectName, List<string> columnsToSelect, IEnumerable<Join> joins)
        {

            if (joins == null || joins.Count() == 0)
                return null;

            List<string> joinClauseList = new List<string>();
            foreach (Join join in joins)
            {

                string alias = getObjectName($"join_table_{getGuidString(join.Id)}");

                string joinPart = "join";
                if (join.JoinType == JoinType.LEFT)
                    joinPart = "left join";
                else if (join.JoinType == JoinType.RIGHT)
                    joinPart = "right join";
                else if (join.JoinType == JoinType.FULL_OUTER)
                    joinPart = "full outer join";

                joinClauseList.Add($"{joinPart} {getObjectName(join.RightTable)} as {alias} on {tableObjectName}.{getObjectName(join.LeftKey)} = {alias}.{getObjectName(join.RightKey)}");

                if (join.FieldToReceiveIsObject)
                {
                    var receiveObject = db.GetTable(((PropertyInfo)join.FieldToReceive).PropertyType);
                    var columns = receiveObject.columns.Select(x => $"{alias}.{getObjectName(x)} as {getObjectName($"receive_{x.Name}_{getGuidString(join.Id)}")}");
                    columnsToSelect.AddRange(columns);
                }
                else
                    columnsToSelect.Add($"{alias}.{getObjectName(join.FieldToSelect)} as {getObjectName(join.FieldToReceive)}");

            }

            return string.Join("\r\n", joinClauseList);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="tableObjectName"></param>
        /// <param name="where"></param>
        /// <param name="addWhere"></param>
        /// <returns></returns>
        public override string generateWhereClause(DbCommand cmd, string tableObjectName, Expression where, bool addWhere = true)
        {

            if (where == null)
                return null;

            SQLExpressionTranslator translater = new SQLExpressionTranslator();
            return (addWhere ? "where " : "") + translater.GenerateSQLExpression(db, cmd, where, tableObjectName);

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public override string getDbType(Type type)
        {
            return dbTypeMap[type];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string getGuidString(Guid guid)
        {
            return guid.ToString("n");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="member"></param>
        /// <returns></returns>
        public override string getObjectName(MemberInfo member)
        {
            return getObjectName(CocoonORM.GetName(member));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public override string getObjectName(string name)
        {
            return $"[{name}]";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="model"></param>
        /// <param name="objectToInsert"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public override T insert<T>(Type model, object objectToInsert, int timeout)
        {

            TableDefinition def = db.GetTable(model);

            using (DbConnection conn = getConnection())
            using (DbCommand cmd = getCommand(conn, timeout))
            {

                //get columns and values
                List<string> columns = new List<string>();
                List<string> values = new List<string>();
                foreach (PropertyInfo prop in objectToInsert.GetType().GetProperties())
                    if (def.columns.Contains(prop) && !ORMUtilities.HasAttribute<IgnoreOnInsert>(prop))
                    {

                        columns.Add($"{def.objectName}.{getObjectName(prop)}");

                        object value = prop.GetValue(objectToInsert);

                        DbParameter param = addParam(cmd, "insert_value_" + getGuidString(Guid.NewGuid()), value);
                        values.Add(param.ParameterName);

                    }

                if (def.primaryKeys.Count > 0)
                {

                    //get primary keys
                    List<string> outputTableKeys = new List<string>();
                    List<string> insertedPrimaryKeys = new List<string>();
                    List<string> wherePrimaryKeys = new List<string>();
                    foreach (PropertyInfo pk in def.primaryKeys)
                    {
                        string primaryKeyName = getObjectName(pk);
                        outputTableKeys.Add($"{primaryKeyName} {getDbType(pk.PropertyType)}");
                        insertedPrimaryKeys.Add("inserted." + primaryKeyName);
                        wherePrimaryKeys.Add($"ids.{primaryKeyName} = {def.objectName}.{primaryKeyName}");
                    }

                    //generate query
                    string insertedTable = $"declare @ids table({string.Join(", ", outputTableKeys)})";
                    cmd.CommandText = $@"{insertedTable};
                    insert into {def.objectName} ({string.Join(", ", columns)}) 
                    output {string.Join(", ", insertedPrimaryKeys)} into @ids values ({string.Join(", ", values)});
                    select {def.objectName}.* from {def.objectName} join @ids ids on {string.Join(" and ", wherePrimaryKeys)}";

                }
                else
                    cmd.CommandText = $@"insert into {def.objectName} ({string.Join(", ", columns)}) values ({string.Join(", ", values)})";

                conn.Open();

                if (typeof(T).IsClass)
                    return readSingle<T>(cmd, null);
                else
                    return readScalar<T>(cmd);

            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="type"></param>
        /// <param name="list"></param>
        /// <param name="joins"></param>
        public override void readList(DbCommand cmd, Type type, List<object> list, IEnumerable<Join> joins)
        {

            using (DbDataReader reader = cmd.ExecuteReader())
                if (reader.HasRows)
                    while (reader.Read())
                        list.Add(readObject(type, reader, joins));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="reader"></param>
        /// <param name="joins"></param>
        /// <returns></returns>
        public override object readObject(Type type, DbDataReader reader, IEnumerable<Join> joins)
        {

            if (type == typeof(object))
            {
                ExpandoObject objectToSet = new ExpandoObject();
                IDictionary<string, object> dict = objectToSet as IDictionary<string, object>;

                foreach (string column in Enumerable.Range(0, reader.FieldCount).Select(reader.GetName))
                    dict.Add(column, reader[column]);

                return objectToSet;
            }
            else
            {
                object obj = Activator.CreateInstance(type);
                ORMUtilities.SetFromReader(obj, reader, joins);
                return obj;
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cmd"></param>
        /// <returns></returns>
        public override T readScalar<T>(DbCommand cmd)
        {
            object v = cmd.ExecuteScalar();
            return v == DBNull.Value || v == null ? default(T) : (T)ORMUtilities.ChangeType(v, typeof(T));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cmd"></param>
        /// <param name="list"></param>
        public override void readScalarList<T>(DbCommand cmd, List<T> list)
        {
            using (DbDataReader reader = cmd.ExecuteReader())
                if (reader.HasRows)
                    while (reader.Read())
                        list.Add(reader.GetFieldValue<T>(0));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cmd"></param>
        /// <param name="joins"></param>
        /// <returns></returns>
        public override T readSingle<T>(DbCommand cmd, IEnumerable<Join> joins)
        {
            using (DbDataReader reader = cmd.ExecuteReader(CommandBehavior.SingleRow))
                if (reader.HasRows)
                {
                    reader.Read();
                    return (T)readObject(typeof(T), reader, joins);
                }

            return default(T);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="cmd"></param>
        /// <param name="tableObjectName"></param>
        /// <param name="columns"></param>
        /// <param name="joins"></param>
        /// <param name="customColumns"></param>
        /// <param name="customParams"></param>
        /// <param name="top"></param>
        /// <param name="distinct"></param>
        /// <param name="where"></param>
        public override void select(DbConnection conn, DbCommand cmd, string tableObjectName, List<PropertyInfo> columns, IEnumerable<Join> joins, IEnumerable<MemberInfo> customColumns, object customParams, int top, bool distinct, Expression where)
        {

            //get columns to select
            List<string> columnsToSelect = columns.Where(c => !ORMUtilities.HasAttribute<IgnoreOnSelect>(c)).Select(c => $"{tableObjectName}.{getObjectName(c)}").ToList();
            if (columnsToSelect.Count == 0)
                throw new Exception("No columns to select");

            //handle custom columns
            if (customColumns != null && customColumns.Count() > 0)
            {

                addParam(cmd, "table", tableObjectName);

                if (customParams != null)
                    addParamObject(cmd, customParams);

                foreach (MemberInfo customColumn in customColumns)
                {

                    CustomColumn attr = customColumn.GetCustomAttribute<CustomColumn>();

                    columnsToSelect.Add($"({attr.sql}) as {getObjectName(customColumn)}");

                }

            }

            //generate join clause
            string joinClause = generateJoinClause(tableObjectName, columnsToSelect, joins);

            //generate where clause
            string whereClause = generateWhereClause(cmd, tableObjectName, where);

            //generate top clause
            string topClause = "";
            if (top > 0)
                topClause = $"top {top}";

            //generate sql
            cmd.CommandText = $"select {(distinct ? "distinct" : "")} {topClause} {string.Join(", ", columnsToSelect)} from {tableObjectName} {joinClause} {whereClause}";

            //execute sql
            conn.Open();

        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="tableObjectName"></param>
        /// <param name="updateFields"></param>
        /// <param name="timeout"></param>
        /// <param name="where"></param>
        /// <returns></returns>
        public override int update<T>(string tableObjectName, IEnumerable<Tuple<PropertyInfo, object>> updateFields, int timeout, Expression<Func<T, bool>> where = null)
        {

            using (DbConnection conn = getConnection())
            using (DbCommand cmd = getCommand(conn, timeout))
            {

                //columns to update
                List<string> columnsToUpdate = new List<string>();
                List<string> primaryKeys = new List<string>();
                string whereClause = generateWhereClause(cmd, tableObjectName, where);
                foreach (Tuple<PropertyInfo, object> field in updateFields)
                {

                    PropertyInfo prop = field.Item1;
                    object value = field.Item2;

                    if (value is SQLValue)
                        columnsToUpdate.Add($"{tableObjectName}.{getObjectName(prop)} = ({((SQLValue)value).sql})");
                    else
                    {

                        if (value is string && string.IsNullOrEmpty((string)value))
                            value = null;

                        DbParameter param = addParam(cmd, "update_field_" + getGuidString(Guid.NewGuid()), value);
                        columnsToUpdate.Add($"{tableObjectName}.{getObjectName(prop)} = {param.ParameterName}");

                    }

                    if (ORMUtilities.HasAttribute<PrimaryKey>(prop) && where == null)
                    {
                        DbParameter param = addParam(cmd, "where_" + getGuidString(Guid.NewGuid()), value);
                        primaryKeys.Add($"{tableObjectName}.{getObjectName(prop)} = {param.ParameterName}");
                    }

                }

                if (where == null)
                    whereClause = "where " + string.Join(" and ", primaryKeys);

                //generate sql
                cmd.CommandText = $"update {tableObjectName} set {string.Join(", ", columnsToUpdate)} {whereClause}";

                //execute
                conn.Open();
                return cmd.ExecuteNonQuery();

            }

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

    }
}
