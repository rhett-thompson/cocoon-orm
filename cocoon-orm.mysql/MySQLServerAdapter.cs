using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using Cocoon.Annotations;
using MySql.Data.MySqlClient;

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
