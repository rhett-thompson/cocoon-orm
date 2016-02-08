using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq.Expressions;
using System.Reflection;

namespace Cocoon
{

    /// <summary>
    /// 
    /// </summary>
    public class SQLServerAdapter : DBServerAdapter
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
            a.connection = new SqlConnection(connectionString);
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

                    SqlCommandBuilder.DeriveParameters((SqlCommand)conn.command);
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

            using (SqlDataAdapter da = new SqlDataAdapter((SqlCommand)conn.command))
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
            get { return _dbTypeMap; }
        }

        private static readonly Dictionary<Type, string> _dbTypeMap = new Dictionary<Type, string>
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

            return "[" + name + "]";

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

            string tableObjectName = getObjectName(tableName);

            List<string> insertedPrimaryKeys = new List<string>();
            foreach (PropertyInfo pk in primaryKeys)
                insertedPrimaryKeys.Add("inserted." + getObjectName(Utilities.GetColumnName(pk)));
  
            return string.Format("insert into {0} ({1}) output {2} into @ids values ({3})",
                tableObjectName,
                string.Join(", ", columns),
                string.Join(", ", insertedPrimaryKeys),
                string.Join(", ", values));

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="primaryKeys"></param>
        /// <returns></returns>
        public override string insertInitSQL(string tableName, List<PropertyInfo> primaryKeys)
        {

            List<string> primaryKeysForInsertTable = new List<string>();
            foreach (PropertyInfo pk in primaryKeys)
                primaryKeysForInsertTable.Add(string.Format("{0} {1}", getObjectName(Utilities.GetColumnName(pk)), csToDBTypeMap[pk.PropertyType]));

            return string.Format("declare @ids table({0})", string.Join(", ", primaryKeysForInsertTable));

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

            List<string> wherePrimaryKeys = new List<string>();
            foreach (PropertyInfo pk in primaryKeys)
                wherePrimaryKeys.Add(string.Format("ids.{0} = {1}.{0}", getObjectName(Utilities.GetColumnName(pk)), tableName));

            return string.Format("select {0}.* from {0} join @ids ids on {1}", tableName, string.Join(" and ", wherePrimaryKeys));

            //return string.Format("select {0}.* from {0} where {1}", tableName, whereClause);

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
                topClause = string.Format("top {0}", top);

            return string.Format("select {0} {1} from {2} {3} {4}",
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
            return whereClause.Replace("@", "@" + paramPrefix);
        }
        
    }
}
