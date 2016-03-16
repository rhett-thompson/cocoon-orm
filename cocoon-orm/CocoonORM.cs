using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Cocoon.ORM
{
    public class CocoonORM
    {

        public string ConnectionString;

        internal Dictionary<Type, TableDefinition> tables = new Dictionary<Type, TableDefinition>();

        public CocoonORM(string connectionString)
        {

            ConnectionString = connectionString;

        }

        #region Basic CRUD

        public List<T> GetList<T>(Expression<Func<T, bool>> where = null, int top = 0)
        {

            TableDefinition def = getTable(typeof(T));
            List<T> list = new List<T>();

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = conn.CreateCommand())
            {
                select(conn, cmd, def.objectName, def.columns, def.foreignColumns, top, where);
                readList<T>(cmd, list);
            }

            return list;

        }

        public T GetSingle<T>(Expression<Func<T, bool>> where)
        {

            TableDefinition def = getTable(typeof(T));

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = conn.CreateCommand())
            {
                select(conn, cmd, def.objectName, def.columns, def.foreignColumns, 1, where);
                return readSingle<T>(cmd);
            }
            
        }

        public FieldT GetScalar<ModelT, FieldT>(Expression<Func<ModelT, FieldT>> fieldToSelect, Expression<Func<ModelT, bool>> where = null)
        {

            MemberExpression expression = (MemberExpression)fieldToSelect.Body;
            TableDefinition def = getTable(expression.Member.DeclaringType);

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = conn.CreateCommand())
            {
                select(conn, cmd, def.objectName, new List<MemberInfo>() { expression.Member }, null, 1, where);
                return readScalar<FieldT>(cmd);

            }

        }

        public List<FieldT> GetScalarList<ModelT, FieldT>(Expression<Func<ModelT, FieldT>> fieldToSelect, Expression<Func<ModelT, bool>> where = null, int top = 0)
        {

            MemberExpression expression = (MemberExpression)fieldToSelect.Body;
            TableDefinition def = getTable(expression.Member.DeclaringType);

            List<FieldT> list = new List<FieldT>();

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = conn.CreateCommand())
            {
                select(conn, cmd, def.objectName, new List<MemberInfo>() { expression.Member }, null, top, where);
                readScalarList(cmd, list);
            }


            return list;

        }

        public int Delete<T>(Expression<Func<T, bool>> where)
        {
            TableDefinition def = getTable(typeof(T));

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = conn.CreateCommand())
            {

                string whereClause = generateWhereClause(cmd, def.objectName, where);

                cmd.CommandText = string.Format("delete from {0} {1}", def.objectName, whereClause);

                conn.Open();
                return cmd.ExecuteNonQuery();

            }
        }

        public int Update<T>(T objectToUpdate, Expression<Func<T, bool>> where = null)
        {
            return Update<T>(objectToUpdate, where);
        }

        public int Update<T>(object fieldsToUpdate, Expression<Func<T, bool>> where = null)
        {

            TableDefinition def = getTable(typeof(T));

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = conn.CreateCommand())
            {

                //columns to select
                List<string> columnsToUpdate = new List<string>();
                foreach (PropertyInfo prop in fieldsToUpdate.GetType().GetProperties())
                    if (!Utilities.HasAttribute<IgnoreOnUpdate>(prop))
                    {
                        SqlParameter param = addParam(cmd, "update_field_" + getGuid(), prop.GetValue(fieldsToUpdate));
                        columnsToUpdate.Add(string.Format("{0}.{1} = {2}", def.objectName, getObjectName(prop), param.ParameterName));
                    }

                //generate where clause
                string whereClause = generateWhereClause(cmd, def.objectName, where);

                //generate sql
                cmd.CommandText = string.Format("update {0} set {1} {2}", def.objectName, string.Join(", ", columnsToUpdate), whereClause);

                //execute
                conn.Open();
                return cmd.ExecuteNonQuery();

            }
        }
        
        public T Insert<T>(T objectToInsert)
        {

            TableDefinition def = getTable(typeof(T)); ;

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = conn.CreateCommand())
            {

                //get columns and values
                List<string> columns = new List<string>();
                List<string> values = new List<string>();
                foreach (PropertyInfo prop in objectToInsert.GetType().GetProperties())
                    if (def.columns.Contains(prop) && !Utilities.HasAttribute<IgnoreOnInsert>(prop))
                    {

                        columns.Add(string.Format("{0}.{1}", def.objectName, getObjectName(prop)));

                        SqlParameter param = addParam(cmd, "insert_value_" + Guid.NewGuid().ToString("n"), prop.GetValue(objectToInsert));
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

        #endregion

        #region SQL

        public List<T> ExecuteSQLList<T>(string sql, object parameters = null)
        {

            bool isScalar = Utilities.HasAttribute<Table>(typeof(T));
            List<T> list = new List<T>();

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = conn.CreateCommand())
            {

                addParamObject(cmd, parameters);

                cmd.CommandText = sql;
                conn.Open();

                if (isScalar)
                    readScalarList(cmd, list);
                else
                    readList(cmd, list);

            }

            return list;

        }

        public T ExecuteSQLSingle<T>(string sql, object parameters = null)
        {

            bool isScalar = Utilities.HasAttribute<Table>(typeof(T));

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = conn.CreateCommand())
            {

                addParamObject(cmd, parameters);

                cmd.CommandText = sql;
                conn.Open();

                if (isScalar)
                    return readScalar<T>(cmd);
                else
                    return readSingle<T>(cmd);

            }
            
        }
        
        public int ExecuteSQLVoid(string sql, object parameters = null)
        {

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = conn.CreateCommand())
            {

                addParamObject(cmd, parameters);

                cmd.CommandText = sql;
                conn.Open();

                return cmd.ExecuteNonQuery();

            }

        }

        #endregion

        #region Stored Procedures

        public List<T> ExecuteProcList<T>(string procedureName, object parameters = null)
        {

            bool isScalar = Utilities.HasAttribute<Table>(typeof(T));
            List<T> list = new List<T>();

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = conn.CreateCommand())
            {

                addProcParams(conn, cmd, parameters);

                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = procedureName;
                conn.Open();

                if (isScalar)
                    readScalarList(cmd, list);
                else
                    readList(cmd, list);

            }

            return list;

        }

        public T ExecuteProcSingle<T>(string procedureName, object parameters = null)
        {

            bool isScalar = Utilities.HasAttribute<Table>(typeof(T));

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = conn.CreateCommand())
            {

                addProcParams(conn, cmd, parameters);

                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = procedureName;
                conn.Open();

                if (isScalar)
                    return readScalar<T>(cmd);
                else
                    return readSingle<T>(cmd);

            }
            
        }

        public int ExecuteProcVoid(string procedureName, object parameters = null)
        {

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = conn.CreateCommand())
            {

                addProcParams(conn, cmd, parameters);

                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = procedureName;
                conn.Open();

                return cmd.ExecuteNonQuery();

            }

        }

        #endregion

        #region Internal

        internal Dictionary<string, SqlParameterCollection> paramCache = new Dictionary<string, SqlParameterCollection>();

        internal void discoverParams(SqlConnection conn, SqlCommand cmd)
        {

            lock (cmd)
            {

                if (!paramCache.ContainsKey(cmd.CommandText))
                {

                    SqlCommandBuilder.DeriveParameters(cmd);
                    paramCache.Add(cmd.CommandText, cmd.Parameters);

                }
                else
                {
                    //IDataParameter[] cachedParams = paramCache[cmd.CommandText].Cast<ICloneable>().Select(p => p.Clone() as IDataParameter).Where(p => p != null).ToArray();
                    foreach (SqlParameter param in paramCache[cmd.CommandText])
                        if (param != null)
                            cmd.Parameters.Add(param);
                }

            }

        }

        internal void addProcParams(SqlConnection conn, SqlCommand cmd, object parameters)
        {

            if (parameters == null)
                return;

            discoverParams(conn, cmd);

            PropertyInfo[] props = parameters.GetType().GetProperties();
            foreach (PropertyInfo prop in props)
            {

                string propName = getObjectName(prop);
                string paramName = "@" + propName;

                if (cmd.Parameters.Contains(paramName))
                    cmd.Parameters[paramName].Value = prop.GetValue(parameters);
                else
                    throw new Exception(string.Format("Invalid parameter ({0}) for stored procedure {1}", propName, cmd.CommandText));

            }

        }

        internal void addParamObject(SqlCommand cmd, object parameters)
        {

            if (parameters != null)
                foreach (PropertyInfo prop in parameters.GetType().GetProperties())
                    addParam(cmd, prop.Name, prop.GetValue(parameters));

        }

        internal void readList<T>(SqlCommand cmd, List<T> list)
        {

            using (SqlDataReader reader = cmd.ExecuteReader())
                if (reader.HasRows)
                    while (reader.Read())
                        list.Add(readObject<T>(reader));

        }

        internal T readSingle<T>(SqlCommand cmd)
        {
            using (SqlDataReader reader = cmd.ExecuteReader(CommandBehavior.SingleRow))
                if (reader.HasRows)
                {
                    reader.Read();
                    return readObject<T>(reader);
                }

            return default(T);

        }

        internal T readObject<T>(SqlDataReader reader)
        {

            T obj = (T)Activator.CreateInstance(typeof(T));
            Utilities.SetFromReader(obj, reader);
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
            List<string> columnsToSelect = columns.Where(c => !Utilities.HasAttribute<IgnoreOnSelect>(c)).Select(c => string.Format("{0}.{1}", tableObjectName, getObjectName(c))).ToList();
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

        internal TableDefinition getTable(Type type)
        {

            if (tables.ContainsKey(type))
                return tables[type];

            TableDefinition table = new TableDefinition();
            table.objectName = getObjectName(type);
            table.type = type;

            foreach (var prop in type.GetProperties())
            {

                if (Utilities.HasAttribute<Column>(prop))
                    table.columns.Add(prop);

                if (Utilities.HasAttribute<ForeignColumn>(prop))
                    table.foreignColumns.Add(prop);

                if (Utilities.HasAttribute<PrimaryKey>(prop))
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
            if (Utilities.HasAttribute<OverrideName>(member))
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
            param.Value = value == null ? DBNull.Value : value;

            return cmd.Parameters.Add(param);

        }

        internal static SqlParameter addWhereParam(SqlCommand cmd, object value)
        {

            return addParam(cmd, string.Format("where_{0}", getGuid()), value);

        }

        internal string generateWhereClause(SqlCommand cmd, string tableObjectName, Expression where)
        {

            if (where == null)
                return null;

            SQLExpressionTranslator translater = new SQLExpressionTranslator();
            return "where " + translater.GenerateSQLExpression(this, cmd, where);

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

                columnsToSelect.Add(string.Format("{0}.{1} as {1}",
                    alias,
                    getObjectName(foreignColumn)));

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
