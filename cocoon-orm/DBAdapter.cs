using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;

namespace Cocoon
{
    internal class DBAdapter : IDisposable
    {

        private Dictionary<string, IDataParameterCollection> paramCache = new Dictionary<string, IDataParameterCollection>();

        public IDbConnection connection { get; set; }
        public IDbCommand command { get; set; }

        public DBAdapter(DBAdapterTarget target, string connectionString)
        {

            if (target == DBAdapterTarget.SQLServer)
                connection = new SqlConnection(connectionString);
            //else if (target == DBAdapterTarget.MySQL)
            //    connection = new MySqlConnection(connectionString);
            else
                throw new Exception("Invalid server type.");

            command = connection.CreateCommand();

        }

        public void Dispose()
        {

            command.Dispose();
            connection.Dispose();

        }

        private void discoverParams()
        {

            lock (command)
            {

                if (!paramCache.ContainsKey(command.CommandText))
                {

                    if (command is SqlCommand)
                        SqlCommandBuilder.DeriveParameters((SqlCommand)command);
                    else
                        throw new Exception("Cannot discover parameters from this database type.");

                    paramCache.Add(command.CommandText, command.Parameters);

                }
                else
                {

                    IDataParameter[] cachedParams = paramCache[command.CommandText].Cast<ICloneable>().Select(p => p.Clone() as IDataParameter).Where(p => p != null).ToArray();
                    foreach (IDataParameter param in cachedParams)
                        command.Parameters.Add(param);

                }

            }

        }

        public void addSProcParams(object paramObject)
        {

            if (paramObject == null)
                return;

            discoverParams();

            PropertyInfo[] props = paramObject.GetType().GetProperties();
            foreach (PropertyInfo prop in props)
            {

                string propName = DBConnection.getColumnName(prop);
                string paramName = "@" + propName;

                if (command.Parameters.Contains(paramName))
                    ((IDbDataParameter)command.Parameters[paramName]).Value = prop.GetValue(paramObject);
                else
                    throw new Exception(string.Format("Invalid parameter ({0}) for stored procedure {1}", propName, command.CommandText));

            }

        }

        public void addParams(object valueObject, string paramPrefix)
        {

            if (valueObject == null)
                return;

            PropertyInfo[] whereProperties = valueObject.GetType().GetProperties();
            addParams(new List<PropertyInfo>(whereProperties), valueObject, paramPrefix);

        }

        public void addParams(List<PropertyInfo> properties, object valueObject, string paramPrefix)
        {

            if (valueObject == null)
                return;

            foreach (PropertyInfo prop in properties)
            {
                object value = prop.GetValue(valueObject);

                IDbDataParameter param = command.CreateParameter();
                param.ParameterName = paramPrefix + DBConnection.getColumnName(prop);
                param.Value = value == null ? DBNull.Value : value;

                command.Parameters.Add(param);
            }

        }

        public int fillDataSet(out DataSet ds)
        {

            if (command is SqlCommand)
            {
                using (SqlDataAdapter da = new SqlDataAdapter((SqlCommand)command))
                {

                    ds = new DataSet();
                    return da.Fill(ds);

                }
            }
            else
                throw new Exception("Cannot fill dataset with this target database.");

        }

        public void openSProc(string procedureName)
        {

            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = procedureName;
            connection.Open();

        }

        public void openSQL(string sql)
        {

            command.CommandText = sql;
            connection.Open();

        }

    }

    /// <summary>
    /// 
    /// </summary>
    internal enum DBAdapterTarget
    {

        /// <summary>
        /// 
        /// </summary>
        SQLServer,

        /// <summary>
        /// 
        /// </summary>
        MySQL

    }

}
