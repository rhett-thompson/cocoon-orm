using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq.Expressions;
using System.Reflection;

namespace Cocoon.ORM
{

    /// <summary>
    /// 
    /// </summary>
    public abstract class SQLPlatform
    {

        /// <summary>
        /// 
        /// </summary>
        public CocoonORM db { get; set; }
        
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public abstract DbConnection getConnection();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public virtual DbCommand getCommand(DbConnection conn, int timeout)
        {

            DbCommand cmd = conn.CreateCommand();
            cmd.CommandTimeout = timeout < 0 ? db.CommandTimeout : timeout;

            return cmd;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        public abstract DbDataAdapter getDataAdapter(DbCommand cmd);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="parameters"></param>
        public abstract void addParamObject(DbCommand cmd, object parameters);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="type"></param>
        /// <param name="list"></param>
        /// <param name="joins"></param>
        public abstract void readList(DbCommand cmd, Type type, List<object> list, IEnumerable<Join> joins);

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cmd"></param>
        /// <param name="joins"></param>
        /// <returns></returns>
        public abstract T readSingle<T>(DbCommand cmd, IEnumerable<Join> joins);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="reader"></param>
        /// <param name="joins"></param>
        /// <returns></returns>
        public abstract object readObject(Type type, DbDataReader reader, IEnumerable<Join> joins);

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cmd"></param>
        /// <param name="list"></param>
        public abstract void readScalarList<T>(DbCommand cmd, List<T> list);

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cmd"></param>
        /// <returns></returns>
        public abstract T readScalar<T>(DbCommand cmd);

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
        public abstract void select(DbConnection conn, DbCommand cmd, string tableObjectName, List<PropertyInfo> columns, IEnumerable<Join> joins, IEnumerable<MemberInfo> customColumns, object customParams, int top, bool distinct, Expression where);

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="tableObjectName"></param>
        /// <param name="updateFields"></param>
        /// <param name="timeout"></param>
        /// <param name="where"></param>
        /// <returns></returns>
        public abstract int update<T>(string tableObjectName, IEnumerable<Tuple<PropertyInfo, object>> updateFields, int timeout, Expression<Func<T, bool>> where = null);

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="model"></param>
        /// <param name="objectToInsert"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public abstract T insert<T>(Type model, object objectToInsert, int timeout);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="member"></param>
        /// <returns></returns>
        public abstract string getObjectName(MemberInfo member);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public abstract string getObjectName(string name);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public abstract string getGuidString();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public abstract DbParameter addParam(DbCommand cmd, string name, object value);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public abstract string addWhereParam(DbCommand cmd, object value);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="tableObjectName"></param>
        /// <param name="where"></param>
        /// <param name="addWhere"></param>
        /// <returns></returns>
        public abstract string generateWhereClause(DbCommand cmd, string tableObjectName, Expression where, bool addWhere = true);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tableObjectName"></param>
        /// <param name="columnsToSelect"></param>
        /// <param name="joins"></param>
        /// <returns></returns>
        public abstract string generateJoinClause(string tableObjectName, List<string> columnsToSelect, IEnumerable<Join> joins);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public abstract string getDbType(Type type);
        
    }
    
}
