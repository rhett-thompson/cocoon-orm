using Cocoon.Annotations;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace Cocoon
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class DBServerAdapter
    {

        /// <summary>
        /// 
        /// </summary>
        public DBConnection connection;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public abstract DBServerConnection getConnection(string connectionString);

        /// <summary>
        /// 
        /// </summary>
        protected Dictionary<string, IDataParameterCollection> paramCache = new Dictionary<string, IDataParameterCollection>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="commandText"></param>
        protected virtual void addCachedParamsToConnection(DBServerConnection conn, string commandText)
        {

            IDataParameter[] cachedParams = paramCache[commandText].Cast<ICloneable>().Select(p => p.Clone() as IDataParameter).Where(p => p != null).ToArray();
            foreach (IDataParameter param in cachedParams)
                conn.command.Parameters.Add(param);

        }

        /// <summary>
        /// 
        /// </summary>
        protected abstract void discoverParams(DBServerConnection conn);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="paramObject"></param>
        public virtual void addSProcParams(DBServerConnection conn, object paramObject)
        {

            if (paramObject == null)
                return;

            discoverParams(conn);

            PropertyInfo[] props = paramObject.GetType().GetProperties();
            foreach (PropertyInfo prop in props)
            {

                string propName = connection.getColumnName(prop);
                string paramName = getParamName(propName);

                if (conn.command.Parameters.Contains(paramName))
                    ((IDbDataParameter)conn.command.Parameters[paramName]).Value = prop.GetValue(paramObject);
                else
                    throw new Exception(string.Format("Invalid parameter ({0}) for stored procedure {1}", propName, conn.command.CommandText));

            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="valueObject"></param>
        /// <param name="paramPrefix"></param>
        public virtual void addParams(DBServerConnection conn, object valueObject, string paramPrefix)
        {

            if (valueObject == null)
                return;

            PropertyInfo[] whereProperties = valueObject.GetType().GetProperties();
            addParams(conn, new List<PropertyInfo>(whereProperties), valueObject, paramPrefix);

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="properties"></param>
        /// <param name="valueObject"></param>
        /// <param name="paramPrefix"></param>
        public virtual void addParams(DBServerConnection conn, List<PropertyInfo> properties, object valueObject, string paramPrefix)
        {

            if (valueObject == null)
                return;

            foreach (PropertyInfo prop in properties)
            {
                object value = prop.GetValue(valueObject);

                IDbDataParameter param = conn.command.CreateParameter();
                param.ParameterName = getParamName(paramPrefix + connection.getColumnName(prop));
                param.Value = value == null ? DBNull.Value : value;

                conn.command.Parameters.Add(param);
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="ds"></param>
        /// <returns></returns>
        public abstract int fillDataSet(DBServerConnection conn, out DataSet ds);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="procedureName"></param>
        public virtual void openSProc(DBServerConnection conn, string procedureName)
        {

            conn.command.CommandType = CommandType.StoredProcedure;
            conn.command.CommandText = procedureName;
            conn.connection.Open();

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="sql"></param>
        public virtual void openSQL(DBServerConnection conn, string sql)
        {

            conn.command.CommandText = sql;
            conn.connection.Open();

        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public abstract string getParamName(string name);

        /// <summary>
        /// 
        /// </summary>
        public abstract Dictionary<Type, string> csToDBTypeMap { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public abstract string getObjectName(string name);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public abstract string dropTableSQL(string tableName);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public abstract string tableExistsSQL(string tableName);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="member"></param>
        /// <returns></returns>
        public abstract string getColumnDefinition(MemberInfo member);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="columns"></param>
        /// <param name="primaryKeys"></param>
        /// <param name="foreignKeys"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public abstract string createTableSQL(List<string> columns, List<string> primaryKeys, List<MemberInfo> foreignKeys, string tableName);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="columns"></param>
        /// <param name="values"></param>
        /// <param name="primaryKeys"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public abstract string createLookupTableSQL(List<string> columns, List<KeyValuePair<string, object>> values, List<string> primaryKeys, string tableName);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="columns"></param>
        /// <param name="values"></param>
        /// <param name="primaryKeys"></param>
        /// <returns></returns>
        public abstract string insertSQL(string tableName, List<string> columns, List<string> values, List<PropertyInfo> primaryKeys);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public abstract string verifyLookupTableSQL(string tableName);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public abstract string verifyTableSQL(string tableName);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="columnsToSelect"></param>
        /// <param name="joinClause"></param>
        /// <param name="whereClause"></param>
        /// <param name="top"></param>
        /// <returns></returns>
        public abstract string selectSQL(string tableName, List<string> columnsToSelect, string joinClause, string whereClause, int top);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="whereClause"></param>
        /// <param name="paramPrefix"></param>
        /// <returns></returns>
        public abstract string parseWhereString(string whereClause, string paramPrefix);

    }
}
