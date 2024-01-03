﻿using System;
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

        /// <summary>
        /// The connection string in use by Cocoon
        /// </summary>
        public string ConnectionString;

        /// <summary>
        /// The default timeout in miliseconds of queries
        /// </summary>
        public int CommandTimeout = 15;

        /// <summary>
        /// 
        /// </summary>
        public SQLPlatform Platform;

        internal Dictionary<Type, TableDefinition> tables = new Dictionary<Type, TableDefinition>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connectionString">The connection string of the database to connect to</param>
        /// <param name="platform">The platform to use.  Defaults to TransactSQL if null.</param>
        public CocoonORM(string connectionString, SQLPlatform platform = null)
        {

            if (string.IsNullOrEmpty(connectionString))
                throw new Exception("Invalid connection string.");

            if (platform == null)
                platform = new TransactSQLPlatform();

            ConnectionString = connectionString;

            Platform = platform;
            Platform.db = this;

        }

        /// <summary>
        /// Pings the database to determine connectivity
        /// </summary>
        /// <param name="timeout"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Returns the absolute name of a field in a table model; for use with custom columns.
        /// </summary>
        /// <typeparam name="ModelT"></typeparam>
        /// <param name="field"></param>
        /// <returns></returns>
        public string Field<ModelT>(Expression<Func<ModelT, object>> field)
        {

            return $"{Platform.getObjectName(typeof(ModelT))}.{Platform.getObjectName(GetExpressionProp(field))}";

        }

        /// <summary>
        /// Returns the name of a table model; for use with custom columns.
        /// </summary>
        /// <typeparam name="ModelT"></typeparam>
        /// <returns></returns>
        public string Table<ModelT>()
        {

            return Platform.getObjectName(typeof(ModelT));

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public TableDefinition GetTable(Type type)
        {

            if (tables.ContainsKey(type))
                return tables[type];

            TableDefinition table = new TableDefinition();
            table.objectName = Platform.getObjectName(type);
            table.type = type;

            //get joins
            foreach (FieldInfo field in type.GetFields().Where(w => w.FieldType == typeof(Join) || w.FieldType.GetInterfaces().Contains(typeof(IEnumerable<Join>))))
            {

                if (!field.IsStatic)
                    throw new InvalidMemberException("Join definitions must be declared static", field);

                if (field.FieldType.GetInterfaces().Contains(typeof(IEnumerable<Join>)))
                    table.joins.AddRange((IEnumerable<Join>)field.GetValue(null));
                else if (field.FieldType == typeof(Join))
                    table.joins.Add((Join)field.GetValue(null));
                else
                    throw new InvalidMemberException("Invalid join field", field);

            }

            //get columns
            var props = type.GetProperties().Where(p => !ORMUtilities.HasAttribute<Ignore>(p));
            bool addAllProperties = props.Count(p => ORMUtilities.HasAttribute<Column>(p)) == 0;
            foreach (var prop in props)
            {

                if (table.joins.Any(x => x.FieldToReceive == prop))
                    continue;

                if (ORMUtilities.HasAttribute<CustomColumn>(prop))
                {
                    table.customColumns.Add(prop);
                    continue;
                }

                if (ORMUtilities.HasAttribute<Column>(prop) || addAllProperties)
                    table.columns.Add(prop);

                if (ORMUtilities.HasAttribute<PrimaryKey>(prop))
                    table.primaryKeys.Add(prop);

            }

            

            if (table.columns.Count == 0 && table.primaryKeys.Count == 0)
                throw new Exception($"Model '{type}' has no columns defined.");

            tables.Add(type, table);

            return table;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="field"></param>
        /// <returns></returns>
        public static PropertyInfo GetExpressionProp<T>(Expression<Func<T, object>> field)
        {

            MemberExpression member;

            if (field.Body.GetType() == typeof(UnaryExpression))
            {
                UnaryExpression unary = (UnaryExpression)field.Body;
                member = (MemberExpression)unary.Operand;
            }
            else
                member = (MemberExpression)field.Body;

            return (PropertyInfo)member.Member;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="field"></param>
        /// <returns></returns>
        public static string GetExpressionPropName<T>(Expression<Func<T, object>> field)
        {
            return GetName(GetExpressionProp(field));
        }

        /// <summary>
        /// Retrieves the name of member
        /// </summary>
        /// <param name="member"></param>
        /// <returns></returns>
        public static string GetName(MemberInfo member)
        {

            string name = member.Name;
            if (ORMUtilities.HasAttribute<OverrideName>(member))
                name = member.GetCustomAttribute<OverrideName>().name;

            return name;

        }

    }

    /// <summary>
    /// 
    /// </summary>
    public class SQLValue
    {

        internal string sql;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sql"></param>
        public SQLValue(string sql)
        {
            this.sql = sql;
        }
    }

    internal class InvalidMemberException : Exception
    {

        public InvalidMemberException(string message, MemberInfo member) : base($"{message} => '{member}' in '{member.DeclaringType}'") { }

    }

    /// <summary>
    /// 
    /// </summary>
    public class ListCacheSettings
    {
        /// <summary>
        /// 
        /// </summary>
        public string ID { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public TimeSpan Timeout { get; set; }

    }

}
