using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;

namespace Cocoon.ORM
{
    public partial class CocoonORM
    {
        
        /// <summary>
        /// Deletes records from a table
        /// </summary>
        /// <typeparam name="T">Table model</typeparam>
        /// <param name="where">Where expression to use for the query</param>
        /// <param name="timeout">Timeout in milliseconds of query</param>
        /// <returns>The number of records that were affected</returns>
        public int Delete<T>(Expression<Func<T, bool>> where, int timeout = -1)
        {

            return Delete(typeof(T), where, timeout);

        }

        /// <summary>
        /// Deletes records from a table
        /// </summary>
        /// <typeparam name="T">Table model</typeparam>
        /// /// <param name="model">Table model type</param>
        /// <param name="where">Where expression to use for the query</param>
        /// <param name="timeout">Timeout in milliseconds of query</param>
        /// <returns>The number of records that were affected</returns>
        public int Delete<T>(Type model, Expression<Func<T, bool>> where, int timeout = -1)
        {
            TableDefinition def = GetTable(model);

            using (DbConnection conn = Platform.getConnection())
            using (DbCommand cmd = Platform.getCommand(conn, timeout))
            {

                string whereClause = Platform.generateWhereClause(cmd, def.objectName, where);

                cmd.CommandText = $"delete from {def.objectName} {whereClause}";

                conn.Open();
                return cmd.ExecuteNonQuery();

            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="objectToDelete"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public int Delete(object objectToDelete, int timeout = -1)
        {

            TableDefinition def = GetTable(objectToDelete.GetType());

            using (DbConnection conn = Platform.getConnection())
            using (DbCommand cmd = Platform.getCommand(conn, timeout))
            {

                List<string> primaryKeys = new List<string>();
                foreach (PropertyInfo key in def.primaryKeys)
                    primaryKeys.Add($"{Platform.getObjectName(key)} = {Platform.addWhereParam(cmd, key.GetValue(objectToDelete))}");

                cmd.CommandText = $"delete from {def.objectName} where {string.Join(" and ", primaryKeys)}";

                conn.Open();
                return cmd.ExecuteNonQuery();

            }

        }
        
    }
}
