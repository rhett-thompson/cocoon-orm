using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Cocoon.ORM
{
    public partial class CocoonORM
    {

        /// <summary>
        /// Returns the number of rows that exist for a query
        /// </summary>
        /// <typeparam name="T">Table model</typeparam>
        /// <param name="where">Where expression to use for the query</param>
        /// <param name="timeout">Timeout in milliseconds of query</param>
        /// <returns>The number of rows</returns>
        public int Count<T>(Expression<Func<T, bool>> where = null, int timeout = -1)
        {

            TableDefinition def = GetTable(typeof(T));

            using (DbConnection conn = Platform.getConnection())
            using (DbCommand cmd = Platform.getCommand(conn, timeout))
            {

                //generate where clause
                string whereClause = Platform.generateWhereClause(cmd, def.objectName, where);

                //generate sql
                cmd.CommandText = $"select count(*) from {def.objectName} {whereClause}";

                //execute sql
                conn.Open();
                return Platform.readScalar<int>(cmd);

            }

        }

        /// <summary>
        /// Returns a binary checksum on a query
        /// </summary>
        /// <typeparam name="T">Table model</typeparam>
        /// <param name="where">Where expression to use for the query</param>
        /// <param name="timeout">Timeout in milliseconds of query</param>
        /// <returns>An integer checksum hash</returns>
        public int Checksum<T>(Expression<Func<T, bool>> where = null, int timeout = -1)
        {

            TableDefinition def = GetTable(typeof(T));

            using (DbConnection conn = Platform.getConnection())
            using (DbCommand cmd = Platform.getCommand(conn, timeout))
            {

                //generate where clause
                string whereClause = Platform.generateWhereClause(cmd, def.objectName, where);

                //generate sql
                cmd.CommandText = $"select checksum_agg(binary_checksum(*)) from {def.objectName} {whereClause}";

                //execute sql
                conn.Open();
                return Platform.readScalar<int>(cmd);

            }

        }

        /// <summary>
        /// Determines of rows exist
        /// </summary>
        /// <typeparam name="T">Table model</typeparam>
        /// <param name="where">Where expression to use for the query</param>
        /// <returns>True if rows exists, False otherwise</returns>
        public bool Exists<T>(Expression<Func<T, bool>> where = null)
        {

            return Count(where) > 0;

        }

        /// <summary>
        /// Copies rows into the same table
        /// </summary>
        /// <typeparam name="T">Table model</typeparam>
        /// <param name="where">Where expression to use for the query</param>
        /// <param name="overrideValues"></param>
        /// <param name="timeout">Timeout in milliseconds of query</param>
        /// <returns>The newly inserted rows</returns>
        public IEnumerable<T> Copy<T>(Expression<Func<T, bool>> where, object overrideValues = null, int timeout = -1)
        {
            TableDefinition def = GetTable(typeof(T));

            using (DbConnection conn = Platform.getConnection())
            using (DbCommand cmd = Platform.getCommand(conn, timeout))
            {

                string whereClause = Platform.generateWhereClause(cmd, def.objectName, where);

                //get columns and values
                List<string> columns = new List<string>();
                List<string> values = new List<string>();
                PropertyInfo[] overrideValueProps = overrideValues != null ? overrideValues.GetType().GetProperties() : new PropertyInfo[0];
                foreach (PropertyInfo prop in def.columns)
                    if (!ORMUtilities.HasAttribute<IgnoreOnInsert>(prop))
                    {

                        string column = $"{def.objectName}.{Platform.getObjectName(prop)}";
                        columns.Add(column);

                        PropertyInfo overrideProp = overrideValueProps.SingleOrDefault(p => p.Name == prop.Name);
                        if (overrideProp != null)
                            values.Add(Platform.addParam(cmd, "override_value_" + Platform.getGuidString(Guid.NewGuid()), overrideProp.GetValue(overrideValues)).ParameterName);
                        else
                            values.Add(column);

                    }

                //get primary keys
                List<string> outputTableKeys = new List<string>();
                List<string> insertedPrimaryKeys = new List<string>();
                List<string> wherePrimaryKeys = new List<string>();
                foreach (PropertyInfo pk in def.primaryKeys)
                {
                    string primaryKeyName = Platform.getObjectName(pk);
                    outputTableKeys.Add($"{primaryKeyName} {Platform.getDbType(pk.PropertyType) }");
                    insertedPrimaryKeys.Add("inserted." + primaryKeyName);
                    wherePrimaryKeys.Add($"ids.{primaryKeyName} = {def.objectName}.{primaryKeyName}");
                }

                //build sql
                string insertedTable = $"declare @ids table({string.Join(", ", outputTableKeys)})";
                cmd.CommandText = $"{insertedTable};insert into {def.objectName} ({string.Join(", ", columns)}) output {string.Join(", ", insertedPrimaryKeys)} into @ids select {string.Join(", ", values)} from {def.objectName} {whereClause};select {def.objectName}.* from {def.objectName} join @ids ids on {string.Join(" and ", wherePrimaryKeys)}";

                conn.Open();

                List<object> list = new List<object>();
                Platform.readList(cmd, def.type, list, null);

                return list.Cast<T>();

            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="where"></param>
        /// <param name="top"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public string MD5HashInDB<T>(Expression<Func<T, bool>> where = null, int top = 0, int timeout = -1)
        {

            TableDefinition def = GetTable(typeof(T));

            using (DbConnection conn = Platform.getConnection())
            using (DbCommand cmd = Platform.getCommand(conn, timeout))
            {

                //generate where clause
                string whereClause = Platform.generateWhereClause(cmd, def.objectName, where);

                //generate columns
                List<string> columns = new List<string>();
                foreach (MemberInfo member in def.columns)
                    if (((PropertyInfo)member).PropertyType == typeof(DateTime) || ((PropertyInfo)member).PropertyType == typeof(DateTime?))
                        columns.Add($"format({Platform.getObjectName(member)}, 'MM/dd/yyyy H:mm:ss')");
                    else
                        columns.Add(Platform.getObjectName(member));

                //generate top clause
                string topClause = "";
                if (top > 0)
                    topClause = $"top {top}";

                //generate sql
                cmd.CommandText = $@"
                    declare @hashes table (md5 varchar(32))
                    declare @list varchar(max)
                    insert into @hashes select {topClause} convert(varchar(32), hashbytes('MD5', convert(varchar(1000), concat({string.Join(", ',', ", columns)}))), 2) as md5 from {def.objectName} {whereClause}
                    select @list = coalesce(@list + ',', '') + md5 from @hashes
                    select convert(varchar(32), hashbytes('MD5', @list), 2)
                ";

                //execute sql
                conn.Open();
                return Platform.readScalar<string>(cmd);

            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="ModelT"></typeparam>
        /// <typeparam name="AggT"></typeparam>
        /// <param name="aggregateFunction"></param>
        /// <param name="fieldToAggregate"></param>
        /// <param name="where"></param>
        /// <param name="top"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public AggT Agg<ModelT, AggT>(string aggregateFunction, Expression<Func<ModelT, object>> fieldToAggregate, Expression<Func<ModelT, bool>> where = null, int top = 0, int timeout = -1)
        {

            TableDefinition def = GetTable(typeof(ModelT));

            using (DbConnection conn = Platform.getConnection())
            using (DbCommand cmd = Platform.getCommand(conn, timeout))
            {

                //generate where clause
                string whereClause = Platform.generateWhereClause(cmd, def.objectName, where);

                //generate top clause
                string topClause = "";
                if (top > 0)
                    topClause = $"top {top}";

                //generate sql
                cmd.CommandText = $"select {topClause} {aggregateFunction}({GetExpressionPropName(fieldToAggregate)}) from {def.objectName} {whereClause}";

                //execute sql
                conn.Open();
                return Platform.readScalar<AggT>(cmd);

            }

        }

    }
}
