using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;

namespace Cocoon.ORM
{
    public partial class CocoonORM
    {

        /// <summary>
        /// Executes a SQL statement for a list
        /// </summary>
        /// <typeparam name="T">Table model</typeparam>
        /// <param name="sql">SQL statement string</param>
        /// <param name="parameters">Object containing the parameters of the query. Members of this object that match @Parameter variables in the SQL will be parameterized.</param>
        /// <param name="timeout">Timeout in milliseconds of query</param>
        /// <returns>A list of type T with the result</returns>
        public IEnumerable<T> ExecuteSQLList<T>(string sql, object parameters = null, int timeout = -1)
        {

            List<object> list = new List<object>();

            using (DbConnection conn = Platform.getConnection())
            using (DbCommand cmd = Platform.getCommand(conn, timeout))
            {

                Platform.addParamObject(cmd, parameters);

                cmd.CommandText = sql;
                conn.Open();

                Platform.readList(cmd, typeof(T), list, null);

            }

            return list.Cast<T>();

        }

        /// <summary>
        /// Executes a SQL statement for a list
        /// </summary>
        /// <typeparam name="T">Table model</typeparam>
        /// <param name="sql">SQL statement string</param>
        /// <param name="parameters">Object containing the parameters of the query. Members of this object that match @Parameter variables in the SQL will be parameterized.</param>
        /// <param name="timeout">Timeout in milliseconds of query</param>
        /// <returns>A list of type T with the result</returns>
        public IEnumerable<T> ExecuteSQLScalarList<T>(string sql, object parameters = null, int timeout = -1)
        {

            List<object> list = new List<object>();

            using (DbConnection conn = Platform.getConnection())
            using (DbCommand cmd = Platform.getCommand(conn, timeout))
            {

                Platform.addParamObject(cmd, parameters);

                cmd.CommandText = sql;
                conn.Open();

                Platform.readScalarList(cmd, list);

            }

            return list.Cast<T>();

        }
        
        /// <summary>
        /// Executes a SQL statement for a single row
        /// </summary>
        /// <typeparam name="T">Table model</typeparam>
        /// <param name="sql">SQL statement string</param>
        /// <param name="parameters">Object containing the parameters of the query. Members of this object that match @Parameter variables in the SQL will be parameterized.</param>
        /// <param name="timeout">Timeout in milliseconds of query</param>
        /// <returns>An object of type T with the result</returns>
        public T ExecuteSQLSingle<T>(string sql, object parameters = null, int timeout = -1)
        {

            bool isScalar = !typeof(T).IsClass;

            using (DbConnection conn = Platform.getConnection())
            using (DbCommand cmd = Platform.getCommand(conn, timeout))
            {

                Platform.addParamObject(cmd, parameters);

                cmd.CommandText = sql;
                conn.Open();

                if (isScalar)
                    return Platform.readScalar<T>(cmd);
                else
                    return Platform.readSingle<T>(cmd, null);

            }

        }

        /// <summary>
        /// Executes a SQL statement with no sesult
        /// </summary>
        /// <param name="sql">SQL statement string</param>
        /// <param name="parameters">Object containing the parameters of the query. Members of this object that match @Parameter variables in the SQL will be parameterized.</param>
        /// <param name="timeout">Timeout in milliseconds of query</param>
        /// <returns>The number of records affected by the query</returns>
        public int ExecuteSQLVoid(string sql, object parameters = null, int timeout = -1)
        {

            using (DbConnection conn = Platform.getConnection())
            using (DbCommand cmd = Platform.getCommand(conn, timeout))
            {

                Platform.addParamObject(cmd, parameters);
                cmd.CommandText = sql;
                conn.Open();

                return cmd.ExecuteNonQuery();

            }

        }

        /// <summary>
        /// Executes a SQL statement for DataSet
        /// </summary>
        /// <param name="sql">SQL statement string</param>
        /// <param name="parameters">Object containing the parameters of the query. Members of this object that match @Parameter variables in the SQL will be parameterized.</param>
        /// <param name="timeout">Timeout in milliseconds of query</param>
        /// <returns>A DataSet with the result of the query</returns>
        public DataSet ExecuteSQLDataSet(string sql, object parameters = null, int timeout = -1)
        {

            using (DbConnection conn = Platform.getConnection())
            using (DbCommand cmd = Platform.getCommand(conn, timeout))
            {

                Platform.addParamObject(cmd, parameters);
                cmd.CommandText = sql;

                using (DbDataAdapter adapter = Platform.getDataAdapter(cmd))
                {
                    DataSet ds = new DataSet();
                    adapter.Fill(ds);
                    return ds;
                }

            }

        }

    }
}
