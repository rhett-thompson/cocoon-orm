using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace Cocoon.ORM
{
    public partial class CocoonORM
    {
        
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T">Table model</typeparam>
        /// <param name="procedureName">The name of the procedure to execute</param>
        /// <param name="parameters">An object containing the parameters of the stored procedure</param>
        /// <param name="timeout">Timeout in milliseconds of query</param>
        /// <returns>A list of type T with the result</returns>
        public IEnumerable<T> ExecuteProcList<T>(string procedureName, object parameters = null, int timeout = -1)
        {

            bool isScalar = !typeof(T).IsClass;
            List<object> list = new List<object>();

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandTimeout = timeout < 0 ? CommandTimeout : timeout;
                addParamObject(cmd, parameters);

                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = procedureName;
                conn.Open();

                if (isScalar)
                    readScalarList(cmd, list);
                else
                    readList(cmd, typeof(T), list, null);

            }

            return list.Cast<T>();

        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T">Table model</typeparam>
        /// <param name="procedureName">The name of the procedure to execute</param>
        /// <param name="parameters">An object containing the parameters of the stored procedure</param>
        /// <param name="timeout">Timeout in milliseconds of query</param>
        /// <returns>An object of type T with the result</returns>
        public T ExecuteProcSingle<T>(string procedureName, object parameters = null, int timeout = -1)
        {

            bool isScalar = !typeof(T).IsClass;

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandTimeout = timeout < 0 ? CommandTimeout : timeout;
                addParamObject(cmd, parameters);

                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = procedureName;
                conn.Open();

                if (isScalar)
                    return readScalar<T>(cmd);
                else
                    return readSingle<T>(cmd, null);

            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="procedureName">The name of the procedure to execute</param>
        /// <param name="parameters">An object containing the parameters of the stored procedure</param>
        /// <param name="timeout">Timeout in milliseconds of query</param>
        /// <returns>The number of rows affected</returns>
        public int ExecuteProcVoid(string procedureName, object parameters = null, int timeout = -1)
        {

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandTimeout = timeout < 0 ? CommandTimeout : timeout;
                addParamObject(cmd, parameters);

                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = procedureName;
                conn.Open();

                return cmd.ExecuteNonQuery();

            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="procedureName">The name of the procedure to execute</param>
        /// <param name="parameters">An object containing the parameters of the stored procedure</param>
        /// <param name="timeout">Timeout in milliseconds of query</param>
        /// <returns>A DataSet with the result</returns>
        public DataSet ExecuteProcDataSet(string procedureName, object parameters = null, int timeout = -1)
        {

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandTimeout = timeout < 0 ? CommandTimeout : timeout;
                addParamObject(cmd, parameters);

                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = procedureName;

                using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                {
                    DataSet ds = new DataSet();
                    adapter.Fill(ds);
                    return ds;
                }

            }

        }


    }
}
