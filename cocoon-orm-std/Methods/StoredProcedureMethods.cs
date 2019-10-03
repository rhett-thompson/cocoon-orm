using System.Collections.Generic;
using System.Data;
using System.Data.Common;
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

            using (DbConnection conn = Platform.getConnection())
            using (DbCommand cmd = Platform.getCommand(conn, timeout))
            {

                Platform.addParamObject(cmd, parameters);

                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = procedureName;
                conn.Open();

                if (isScalar)
                    Platform.readScalarList(cmd, list);
                else
                    Platform.readList(cmd, typeof(T), list, null);

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

            using (DbConnection conn = Platform.getConnection())
            using (DbCommand cmd = Platform.getCommand(conn, timeout))
            {

                Platform.addParamObject(cmd, parameters);

                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = procedureName;
                conn.Open();

                if (isScalar)
                    return Platform.readScalar<T>(cmd);
                else
                    return Platform.readSingle<T>(cmd, null);

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

            using (DbConnection conn = Platform.getConnection())
            using (DbCommand cmd = Platform.getCommand(conn, timeout))
            {

                Platform.addParamObject(cmd, parameters);

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

            using (DbConnection conn = Platform.getConnection())
            using (DbCommand cmd = Platform.getCommand(conn, timeout))
            {

                Platform.addParamObject(cmd, parameters);

                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = procedureName;

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
