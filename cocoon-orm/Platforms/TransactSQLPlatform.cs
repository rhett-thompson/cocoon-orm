using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Data.SqlTypes;
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
            return value == null ? "null" : addParam(cmd, string.Format("where_{0}", getGuidString()), value).ParameterName;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tableObjectName"></param>
        /// <param name="columnsToSelect"></param>
        /// <param name="joins"></param>
        /// <returns></returns>
        public override string generateJoinClause(string tableObjectName, List<string> columnsToSelect, IEnumerable<JoinDef> joins)
        {

            if (joins == null || joins.Count() == 0)
                return null;

            List<string> joinClauseList = new List<string>();
            foreach (JoinDef join in joins)
            {

                string alias = getObjectName(string.Format("join_table_{0}", getGuidString()));

                string joinPart = "join";
                if (join.JoinType == JoinType.LEFT)
                    joinPart = "left join";
                else if (join.JoinType == JoinType.RIGHT)
                    joinPart = "right join";
                else if (join.JoinType == JoinType.FULL_OUTER)
                    joinPart = "full outer join";

                joinClauseList.Add("{joinPart} {otherModel} as {alias} on {model}.{key} = {alias}.{otherKey}".Inject(new
                {

                    joinPart = joinPart,
                    model = tableObjectName,
                    otherModel = getObjectName(join.RightTable),
                    alias = alias,
                    key = getObjectName(join.LeftKey),
                    otherKey = getObjectName(join.RightKey)
                }));

                columnsToSelect.Add(string.Format("{0}.{1} as {2}",
                    alias,
                    getObjectName(join.FieldToSelect),
                    getObjectName(join.FieldToRecieve)));

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
        public override string getGuidString()
        {
            return Guid.NewGuid().ToString("n");
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
            return string.Format("[{0}]", name);
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
                    if (def.columns.Contains(prop) && !Utilities.HasAttribute<IgnoreOnInsert>(prop))
                    {

                        columns.Add(string.Format("{0}.{1}", def.objectName, getObjectName(prop)));

                        object value = prop.GetValue(objectToInsert);

                        DbParameter param = addParam(cmd, "insert_value_" + getGuidString(), value);
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
        public override void readList(DbCommand cmd, Type type, List<object> list, IEnumerable<JoinDef> joins)
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
        public override object readObject(Type type, DbDataReader reader, IEnumerable<JoinDef> joins)
        {
            object obj = Activator.CreateInstance(type);
            Utilities.SetFromReader(obj, reader, joins);
            return obj;
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
            return v == DBNull.Value || v == null ? default(T) : (T)v;
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
        public override T readSingle<T>(DbCommand cmd, IEnumerable<JoinDef> joins)
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
        /// <param name="where"></param>
        public override void select(DbConnection conn, DbCommand cmd, string tableObjectName, List<MemberInfo> columns, IEnumerable<JoinDef> joins, IEnumerable<MemberInfo> customColumns, object customParams, int top, Expression where)
        {

            //get columns to select
            List<string> columnsToSelect = columns.Where(c => !Utilities.HasAttribute<IgnoreOnSelect>(c)).Select(c => string.Format("{0}.{1}", tableObjectName, getObjectName(c))).ToList();
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

                    AggSQLColumn attr = customColumn.GetCustomAttribute<AggSQLColumn>();

                    columnsToSelect.Add(string.Format("({0}) as {1}", attr.sql, getObjectName(customColumn)));

                }

            }

            //generate join clause
            string joinClause = generateJoinClause(tableObjectName, columnsToSelect, joins);

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

                    if (!Utilities.HasAttribute<IgnoreOnUpdate>(prop))
                    {

                        if (value is SQLValue)
                            columnsToUpdate.Add(string.Format("{0}.{1} = ({2})", tableObjectName, getObjectName(prop), ((SQLValue)value).sql));
                        else
                        {

                            if (value is string && string.IsNullOrEmpty((string)value))
                                value = null;

                            DbParameter param = addParam(cmd, "update_field_" + getGuidString(), value);
                            columnsToUpdate.Add(string.Format("{0}.{1} = {2}", tableObjectName, getObjectName(prop), param.ParameterName));

                        }

                    }

                    if (Utilities.HasAttribute<PrimaryKey>(prop) && where == null)
                    {
                        DbParameter param = addParam(cmd, "where_" + getGuidString(), value);
                        primaryKeys.Add(string.Format("{0}.{1} = {2}", tableObjectName, getObjectName(prop), param.ParameterName));
                    }

                }

                if (where == null)
                    whereClause = "where " + string.Join(" and ", primaryKeys);

                //generate sql
                cmd.CommandText = string.Format("update {0} set {1} {2}", tableObjectName, string.Join(", ", columnsToUpdate), whereClause);

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
