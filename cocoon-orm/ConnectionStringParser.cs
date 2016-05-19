using System;
using System.Linq;

namespace Cocoon.ORM
{

    /// <summary>
    /// Parses a connection string into its parts
    /// </summary>
    public class ConnectionStringParser
    {

        static readonly string[] serverAliases = { "server", "host", "data source", "datasource", "address",
                                           "addr", "network address" };
        static readonly string[] databaseAliases = { "database", "initial catalog" };
        static readonly string[] usernameAliases = { "user id", "uid", "username", "user name", "user" };
        static readonly string[] passwordAliases = { "password", "pwd" };

        /// <summary>
        /// Gets the password of the connection string
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static string GetPassword(string connectionString)
        {
            return GetValue(connectionString, passwordAliases);
        }

        /// <summary>
        /// Gets the username of the connection string
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static string GetUsername(string connectionString)
        {
            return GetValue(connectionString, usernameAliases);
        }

        /// <summary>
        /// Gets the database name of the connection string
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static string GetDatabaseName(string connectionString)
        {
            return GetValue(connectionString, databaseAliases);
        }

        /// <summary>
        /// Gets the server name of the connection string
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static string GetServerName(string connectionString)
        {
            return GetValue(connectionString, serverAliases);
        }

        internal static string GetValue(string connectionString, params string[] keyAliases)
        {
            var keyValuePairs = connectionString.Split(';')
                                                .Where(kvp => kvp.Contains('='))
                                                .Select(kvp => kvp.Split(new char[] { '=' }, 2))
                                                .ToDictionary(kvp => kvp[0].Trim(),
                                                              kvp => kvp[1].Trim(),
                                                              StringComparer.InvariantCultureIgnoreCase);
            foreach (var alias in keyAliases)
            {
                string value;
                if (keyValuePairs.TryGetValue(alias, out value))
                    return value;
            }
            return string.Empty;
        }

    }
}
