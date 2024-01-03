using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net.NetworkInformation;
using System.Reflection;

namespace Cocoon.ORM
{

    /// <summary>
    /// Database connection
    /// </summary>
    public partial class CocoonORM
    {

        internal Dictionary<Type, TableDefinition> tables = new Dictionary<Type, TableDefinition>();

        public string ConnectionString;

        public CocoonORM(string connectionString)
        {

            ConnectionString = connectionString;

        }

        public PingReply Ping(int timeout = 5000)
        {

            Ping ping = new Ping();
            string server = ConnectionStringParser.GetServerName(ConnectionString);

            if (server.Contains(","))
                server = server.Substring(0, server.LastIndexOf(","));
            else if (server.Contains(":"))
                server = server.Substring(0, server.LastIndexOf(":"));

            return ping.Send(server, timeout);

        }

        public IEnumerable<T> GetList<T>()
        {
            return default(T);
        }

    }

}
