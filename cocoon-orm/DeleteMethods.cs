using System;
using System.Collections.Generic;
using System.Data.SqlClient;
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
            TableDefinition def = getTable(model);

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandTimeout = timeout < 0 ? CommandTimeout : timeout;
                string whereClause = generateWhereClause(cmd, def.objectName, where);

                cmd.CommandText = "delete from {model} {where}".Inject(new { model = def.objectName, where = whereClause });

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

            TableDefinition def = getTable(objectToDelete.GetType());

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandTimeout = timeout < 0 ? CommandTimeout : timeout;

                List<string> primaryKeys = new List<string>();
                foreach (PropertyInfo key in def.primaryKeys)
                    primaryKeys.Add("{key} = {value}".Inject(new { key = getObjectName(key), value = addWhereParam(cmd, key.GetValue(objectToDelete)) }));

                cmd.CommandText = "delete from {model} where {where}".Inject(new { model = def.objectName, where = string.Join(" and ", primaryKeys) });

                conn.Open();
                return cmd.ExecuteNonQuery();

            }

        }
        
    }
}
