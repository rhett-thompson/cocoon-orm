using System;
using System.Collections.Generic;
using System.Data.SqlClient;
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

            TableDefinition def = getTable(typeof(T));

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = conn.CreateCommand())
            {

                cmd.CommandTimeout = timeout < 0 ? CommandTimeout : timeout;

                //generate where clause
                string whereClause = generateWhereClause(cmd, def.objectName, where);

                //generate sql
                cmd.CommandText = "select count(*) from {model} {where}".Inject(new { model = def.objectName, where = whereClause });

                //execute sql
                conn.Open();
                return readScalar<int>(cmd);

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

            TableDefinition def = getTable(typeof(T));

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandTimeout = timeout < 0 ? CommandTimeout : timeout;

                //generate where clause
                string whereClause = generateWhereClause(cmd, def.objectName, where);

                //generate sql
                cmd.CommandText = "select checksum_agg(binary_checksum(*)) from {model} {where}".Inject(new { model = def.objectName, where = whereClause });

                //execute sql
                conn.Open();
                return readScalar<int>(cmd);

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
            TableDefinition def = getTable(typeof(T));

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandTimeout = timeout < 0 ? CommandTimeout : timeout;
                string whereClause = generateWhereClause(cmd, def.objectName, where);

                //get columns and values
                List<string> columns = new List<string>();
                List<string> values = new List<string>();
                PropertyInfo[] overrideValueProps = overrideValues != null ? overrideValues.GetType().GetProperties() : new PropertyInfo[0];
                foreach (PropertyInfo prop in def.columns)
                    if (!Utilities.HasAttribute<IgnoreOnInsert>(prop))
                    {

                        string column = string.Format("{0}.{1}", def.objectName, getObjectName(prop));
                        columns.Add(column);

                        PropertyInfo overrideProp = overrideValueProps.SingleOrDefault(p => p.Name == prop.Name);
                        if (overrideProp != null)
                            values.Add(addParam(cmd, "override_value_" + getGuidString(), overrideProp.GetValue(overrideValues)).ParameterName);
                        else
                            values.Add(column);

                    }

                //get primary keys
                List<string> outputTableKeys = new List<string>();
                List<string> insertedPrimaryKeys = new List<string>();
                List<string> wherePrimaryKeys = new List<string>();
                foreach (PropertyInfo pk in def.primaryKeys)
                {
                    string primaryKeyName = getObjectName(pk);
                    outputTableKeys.Add("{key} {type}".Inject(new { key = primaryKeyName, type = dbTypeMap[pk.PropertyType] }));
                    insertedPrimaryKeys.Add("inserted." + primaryKeyName);
                    wherePrimaryKeys.Add("ids.{primaryKey} = {model}.{primaryKey}".Inject(new { primaryKey = primaryKeyName, model = def.objectName }));
                }

                //build sql
                cmd.CommandText = "{insertedTable};insert into {model} ({columns}) output {insertedPrimaryKeys} into @ids select {values} from {model} {where};select {model}.* from {model} join @ids ids on {wherePrimaryKeys}".Inject(new
                {
                    insertedTable = string.Format("declare @ids table({0})", string.Join(", ", outputTableKeys)),
                    model = def.objectName,
                    columns = string.Join(", ", columns),
                    insertedPrimaryKeys = string.Join(", ", insertedPrimaryKeys),
                    values = string.Join(", ", values),
                    wherePrimaryKeys = string.Join(" and ", wherePrimaryKeys),
                    where = whereClause
                });

                conn.Open();

                List<object> list = new List<object>();
                readList(cmd, def.type, list, null);

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

            TableDefinition def = getTable(typeof(T));

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandTimeout = timeout < 0 ? CommandTimeout : timeout;

                //generate where clause
                string whereClause = generateWhereClause(cmd, def.objectName, where);

                //generate columns
                List<string> columns = new List<string>();
                foreach (MemberInfo member in def.columns)
                    if (((PropertyInfo)member).PropertyType == typeof(DateTime) || ((PropertyInfo)member).PropertyType == typeof(DateTime?))
                        columns.Add("format({column}, 'MM/dd/yyyy H:mm:ss')".InjectSingleValue("column", getObjectName(member)));
                    else
                        columns.Add(getObjectName(member));

                //generate top clause
                string topClause = "";
                if (top > 0)
                    topClause = string.Format("top {0}", top);

                //generate sql
                cmd.CommandText = @"
                    declare @hashes table (md5 varchar(32))
                    declare @list varchar(max)
                    insert into @hashes select {top} convert(varchar(32), hashbytes('MD5', convert(varchar(1000), concat({columns}))), 2) as md5 from {model} {where}
                    select @list = coalesce(@list + ',', '') + md5 from @hashes
                    select convert(varchar(32), hashbytes('MD5', @list), 2)
                ".Inject(new
                {
                    top = topClause,
                    columns = string.Join(", ',', ", columns),
                    model = def.objectName,
                    where = whereClause
                });

                //execute sql
                conn.Open();
                return readScalar<string>(cmd);

            }

        }

    }
}
