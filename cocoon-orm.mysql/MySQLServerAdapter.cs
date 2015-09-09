using Cocoon.Annotations;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Cocoon
{

    /// <summary>
    /// 
    /// </summary>
    public class MySQLServerAdapter : DBServerAdapter
    {

        /// <summary>
        /// 
        /// 
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public override DBServerConnection getConnection(string connectionString)
        {
            DBServerConnection a = new DBServerConnection();

            a.connection = new MySqlConnection(connectionString);
            a.command = a.connection.CreateCommand();
            return a;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="conn"></param>
        protected override void discoverParams(DBServerConnection conn)
        {

            lock (conn.command)
            {

                if (!paramCache.ContainsKey(conn.command.CommandText))
                {

                    MySqlCommandBuilder.DeriveParameters((MySqlCommand)conn.command);
                    paramCache.Add(conn.command.CommandText, conn.command.Parameters);

                }
                else
                    addCachedParamsToConnection(conn, conn.command.CommandText);

            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="ds"></param>
        /// <returns></returns>
        public override int fillDataSet(DBServerConnection conn, out DataSet ds)
        {

            using (MySqlDataAdapter da = new MySqlDataAdapter((MySqlCommand)conn.command))
            {

                ds = new DataSet();
                return da.Fill(ds);

            }

        }

        /// <summary>
        /// 
        /// </summary>
        public override Dictionary<Type, string> csToDBTypeMap
        {
            get { return _csToDBTypeMap; }
        }

        private static readonly Dictionary<Type, string> _csToDBTypeMap = new Dictionary<Type, string>
        {
            {typeof(Int64), "bigint"}, 
            {typeof(UInt64), "bigint"}, 

            {typeof(Byte[]), "varbinary"}, 
            {typeof(Boolean), "bit"}, 
            {typeof(String), "text"}, 
            {typeof(Char), "nchar"}, 
            {typeof(DateTime), "datetime"}, 
            {typeof(DateTimeOffset), "datetimeoffset"}, 
            {typeof(Decimal), "decimal"}, 
            {typeof(Double), "float"}, 
            {typeof(Int32), "int"},
            {typeof(UInt32), "int"},
            {typeof(TimeSpan), "time"},
            {typeof(Guid), "char(36)"},
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
            {typeof(Guid?), "char(36)"},
            {typeof(Int16?), "smallint"},
            {typeof(UInt16?), "smallint"},
            {typeof(Single?), "real"}

        };

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public override string getParamName(string name)
        {
            return "@" + name;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public override string getObjectName(string name)
        {

            return "`" + name + "`";

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public override string dropTableSQL(string tableName)
        {
            return string.Format("drop table if exists {0}", getObjectName(tableName));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public override string tableExistsSQL(string tableName)
        {
            return string.Format("select if(count(*) > 0, 'true', 'false') as `TableExists` from information_schema.tables where table_name = '{0}'", tableName);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="member"></param>
        /// <returns></returns>
        public override string getColumnDefinition(MemberInfo member)
        {

            Column columnAnnotation = member.GetCustomAttribute<Column>(false);
            string columnName = Utilities.GetColumnName(member);

            //data type
            string dataType = "";
            if (!string.IsNullOrEmpty(columnAnnotation.DataType))
                dataType = columnAnnotation.DataType;
            else if (member.MemberType == MemberTypes.Field && csToDBTypeMap.ContainsKey(((FieldInfo)member).FieldType))
                dataType = csToDBTypeMap[((FieldInfo)member).FieldType];
            else if (member.MemberType == MemberTypes.Property && csToDBTypeMap.ContainsKey(((PropertyInfo)member).PropertyType))
                dataType = csToDBTypeMap[((PropertyInfo)member).PropertyType];
            else
                throw new Exception(string.Format("Could not determine data type for column {0} for table {1}.", member.Name, member.ReflectedType.Name));

            //not null
            string notNull = "";
            if (Utilities.HasAttribute<NotNull>(member))
                notNull = "not null";

            //default value
            string defaultValue = "";
            if (Utilities.HasAttribute<Identity>(member))
            {
                Identity identityAnnotation = member.GetCustomAttribute<Identity>(false);
                defaultValue = "auto_increment";
            }
            else if (!string.IsNullOrEmpty(columnAnnotation.DefaultValue))
                defaultValue = string.Format("default {0}", columnAnnotation.DefaultValue);

            //foreign key
            //string foreignKey = "";
            //if (Utilities.HasAttribute<ForeignKey>(member))
            //{

            //    ForeignKey foreignKeyAnnotation = member.GetCustomAttribute<ForeignKey>(false);

            //    if (foreignKeyAnnotation.referencesTable != null)
            //    {

            //        string primaryKeyColumn = columnName;
            //        if (!string.IsNullOrEmpty(foreignKeyAnnotation.referenceTablePrimaryKeyOverride))
            //            primaryKeyColumn = foreignKeyAnnotation.referenceTablePrimaryKeyOverride;

            //        foreignKey = string.Format("foreign key references {0}({1})", connection.getTableName(foreignKeyAnnotation.referencesTable), primaryKeyColumn);
            //    }

            //}

            //generate column
            string column = string.Format("{0} {1} {2} {3}", getObjectName(columnName), dataType, notNull, defaultValue);
            return Regex.Replace(column, @"\s+", " ");

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="columns"></param>
        /// <param name="primaryKeys"></param>
        /// <param name="foreignKeys"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public override string createTableSQL(List<string> columns, List<string> primaryKeys, List<MemberInfo> foreignKeys, string tableName)
        {

            if (primaryKeys.Count > 0)
                columns.Add(string.Format("primary key ({0})", string.Join(", ", primaryKeys)));

            if (foreignKeys.Count > 0)
                foreach (MemberInfo fk in foreignKeys)
                {

                    ForeignKey foreignKeyAnnotation = fk.GetCustomAttribute<ForeignKey>(false);

                    if (foreignKeyAnnotation.ReferencesTable != null)
                    {

                        string foreignKeyColumn = Utilities.GetColumnName(fk);
                        string primaryKeyColumn = foreignKeyColumn;
                        if (!string.IsNullOrEmpty(foreignKeyAnnotation.ReferenceTablePrimaryKeyOverride))
                            primaryKeyColumn = foreignKeyAnnotation.ReferenceTablePrimaryKeyOverride;

                        columns.Add(string.Format("foreign key ({0}) references {1}({2})", 
                            foreignKeyColumn,
                            getObjectName(Utilities.GetTableName(foreignKeyAnnotation.ReferencesTable)), 
                            primaryKeyColumn));

                    }

                    
                }

            return string.Format("create table if not exists {1} ({2})",
                tableName,
                getObjectName(tableName),
                string.Join(", ", columns));

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="columns"></param>
        /// <param name="values"></param>
        /// <param name="primaryKeys"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public override string createLookupTableSQL(List<string> columns, List<KeyValuePair<string, object>> values, List<string> primaryKeys, string tableName)
        {
            List<string> insert = new List<string>();
            foreach (KeyValuePair<string, object> value in values)
                insert.Add(string.Format("insert ignore into {0} ({1}) values ('{2}')", getObjectName(tableName), value.Key, value.Value));

            return string.Format("create table if not exists {1} ({2}); {3}",
                tableName,
                getObjectName(tableName),
                string.Join(", ", columns),
                string.Join("; ", insert));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="columns"></param>
        /// <param name="values"></param>
        /// <param name="primaryKeys"></param>
        /// <returns></returns>
        public override string insertSQL(string tableName, List<string> columns, List<string> values, List<PropertyInfo> primaryKeys)
        {

            tableName = getObjectName(tableName);

            return string.Format("insert into {0} ({1}) values ({2})",
                tableName,
                string.Join(", ", columns),
                string.Join(", ", values));

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="columns"></param>
        /// <param name="values"></param>
        /// <param name="primaryKeys"></param>
        /// <returns></returns>
        public override string insertSelectSQL(string tableName, string whereClause, List<PropertyInfo> primaryKeys)
        {

            tableName = getObjectName(tableName);

            if (primaryKeys.Count == 1 && Utilities.HasAttribute<Identity>(primaryKeys[0]))
                return string.Format("select {0}.* from {0} where {0}.{1} = last_insert_id()", tableName, getObjectName(Utilities.GetColumnName(primaryKeys[0])));
            else
                return string.Format("select {0}.* from {0} where {1}", tableName, whereClause);


        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public override string verifyLookupTableSQL(string tableName)
        {
            return string.Format("select * from {0}", getObjectName(tableName));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public override string verifyTableSQL(string tableName)
        {
            return string.Format("select COLUMN_NAME from INFORMATION_SCHEMA.COLUMNS where TABLE_NAME='{0}'", tableName);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="columnsToSelect"></param>
        /// <param name="joinClause"></param>
        /// <param name="whereClause"></param>
        /// <param name="top"></param>
        /// <returns></returns>
        public override string selectSQL(string tableName, List<string> columnsToSelect, string joinClause, string whereClause, int top)
        {

            string topClause = "";
            if (top > 0)
                topClause = string.Format("limit {0}", top);

            return string.Format("select {1} from {2} {3} {4} {0}",
                topClause,
                string.Join(", ", columnsToSelect),
                getObjectName(tableName),
                joinClause,
                whereClause);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="whereClause"></param>
        /// <param name="paramPrefix"></param>
        /// <returns></returns>
        public override string parseWhereString(string whereClause, string paramPrefix)
        {
            return whereClause.Replace("?", "?" + paramPrefix);
        }

    }
}
