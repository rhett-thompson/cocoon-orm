using System;
using System.Data;

namespace Cocoon
{

    /// <summary>
    /// 
    /// </summary>
    public class DBServerConnection : IDisposable
    {

        /// <summary>
        /// 
        /// </summary>
        public IDbConnection connection;

        /// <summary>
        /// 
        /// </summary>
        public IDbCommand command;

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {

            command.Dispose();
            connection.Dispose();

        }

    }
}
