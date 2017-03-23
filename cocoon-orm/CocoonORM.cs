using System;
using System.Collections;
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
namespace Cocoon.ORM
{

    /// <summary>
    /// Database connection
    /// </summary>
    public partial class CocoonORM
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

            if (string.IsNullOrEmpty(connectionString))
                throw new Exception("Invalid connection string.");

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

        /// <summary>
        /// Returns the absolute name of a field in a table model; for use with custom columns.
        /// </summary>
        /// <typeparam name="ModelT"></typeparam>
        /// <param name="field"></param>
        /// <returns></returns>
        public static string GetObject<ModelT>(Expression<Func<ModelT, object>> field)
        {

            return string.Format("{0}.{1}", getObjectName(typeof(ModelT)), getObjectName(getExpressionProp(field)));

        }

        /// <summary>
        /// Returns the name of a table model; for use with custom columns.
        /// </summary>
        /// <typeparam name="ModelT"></typeparam>
        /// <returns></returns>
        public static string GetObject<ModelT>()
        {

            return getObjectName(typeof(ModelT));

        }

        #region Internal

        internal static PropertyInfo getExpressionProp<ModelT>(Expression<Func<ModelT, object>> field)
        {

            MemberExpression member;

            if (field.Body.GetType() == typeof(UnaryExpression))
            {
                UnaryExpression unary = (UnaryExpression)field.Body;
                member = (MemberExpression)unary.Operand;
            }
            else
                member = (MemberExpression)field.Body;

            return (PropertyInfo)member.Member;

        }

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

        internal void readList(SqlCommand cmd, Type type, List<object> list, IEnumerable<JoinDef> joins)
        {

            using (SqlDataReader reader = cmd.ExecuteReader())
                if (reader.HasRows)
                    while (reader.Read())
                        list.Add(readObject(type, reader, joins));

        }

        internal T readSingle<T>(SqlCommand cmd, IEnumerable<JoinDef> joins)
        {

            using (SqlDataReader reader = cmd.ExecuteReader(CommandBehavior.SingleRow))
                if (reader.HasRows)
                {
                    reader.Read();
                    return (T)readObject(typeof(T), reader, joins);
                }

            return default(T);

        }

        internal object readObject(Type type, SqlDataReader reader, IEnumerable<JoinDef> joins)
        {

            object obj = Activator.CreateInstance(type);
            Utilities.SetFromReader(obj, reader, joins);
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

        internal void select(SqlConnection conn, SqlCommand cmd, string tableObjectName, List<MemberInfo> columns, IEnumerable<JoinDef> joins, IEnumerable<MemberInfo> customColumns, object customParams, int top, Expression where)
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

        internal int update<T>(string tableObjectName, IEnumerable<Tuple<PropertyInfo, object>> updateFields, int timeout, Expression<Func<T, bool>> where = null)
        {

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = conn.CreateCommand())
            {

                cmd.CommandTimeout = timeout < 0 ? CommandTimeout : timeout;

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

                            SqlParameter param = addParam(cmd, "update_field_" + getGuidString(), value);
                            columnsToUpdate.Add(string.Format("{0}.{1} = {2}", tableObjectName, getObjectName(prop), param.ParameterName));

                        }

                    }

                    if (Utilities.HasAttribute<PrimaryKey>(prop) && where == null)
                    {
                        SqlParameter param = addParam(cmd, "where_" + getGuidString(), value);
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
                    if (def.columns.Contains(prop) && !Utilities.HasAttribute<IgnoreOnInsert>(prop))
                    {

                        columns.Add(string.Format("{0}.{1}", def.objectName, getObjectName(prop)));

                        object value = prop.GetValue(objectToInsert);

                        SqlParameter param = addParam(cmd, "insert_value_" + getGuidString(), value);
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

        internal TableDefinition getTable(Type type)
        {

            if (tables.ContainsKey(type))
                return tables[type];

            TableDefinition table = new TableDefinition();
            table.objectName = getObjectName(type);
            table.type = type;

            //get columns
            foreach (var prop in type.GetProperties())
            {

                if (Utilities.HasAttribute<AggSQLColumn>(prop))
                {
                    table.customColumns.Add(prop);
                    continue;
                }

                if (Utilities.HasAttribute<Column>(prop))
                    table.columns.Add(prop);

                if (Utilities.HasAttribute<PrimaryKey>(prop))
                    table.primaryKeys.Add(prop);

            }

            //get joins
            foreach (FieldInfo field in type.GetFields().Where(w => Utilities.HasAttribute<Join>(w)))
            {

                if (!field.IsStatic)
                    throw new InvalidMemberException("Join attribute must decorate static fields only", field);

                if (field.FieldType.GetInterfaces().Contains(typeof(IEnumerable<JoinDef>)))
                    table.joins.AddRange((IEnumerable<JoinDef>)field.GetValue(null));
                else if (field.FieldType == typeof(JoinDef))
                    table.joins.Add((JoinDef)field.GetValue(null));
                else
                    throw new InvalidMemberException("Join attribute must decorate JoinDef field", field);

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
            if (Utilities.HasAttribute<OverrideName>(member))
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

        internal string generateJoinClause(string tableObjectName, List<string> columnsToSelect, IEnumerable<JoinDef> joins)
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
                    otherModel = join.RightTable,
                    alias = alias,
                    key = join.LeftKey,
                    otherKey = join.RightKey
                }));

                columnsToSelect.Add(string.Format("{0}.{1} as {2}",
                    alias,
                    join.FieldToSelect,
                    getObjectName(join.FieldToRecieve)));

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

    /// <summary>
    /// 
    /// </summary>
    public class SQLValue
    {

        internal string sql;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sql"></param>
        public SQLValue(string sql)
        {
            this.sql = sql;
        }
    }
    
    internal class InvalidMemberException : Exception
    {

        public InvalidMemberException(string message, MemberInfo member) : base(string.Format("{0} => '{1}' in '{2}'", message, member, member.DeclaringType)) { }

    }

}
