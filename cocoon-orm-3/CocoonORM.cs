﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace Cocoon3
{

    internal enum QueryType
    {
        SelectNormal,
        SelectCascade,
        Update,
        Insert,
        Delete
    }

    public enum JoinType
    {

        Left,
        Inner,
        Right,
        FullOuter

    }

    internal class Query
    {

        public QueryType type;
        public Type modelType;
        public Type resultType;
        public Type parentType;

        public LambdaExpression where;

        public IEnumerable<LambdaExpression> selectFields;
        public int selectTop;
        public bool selectDistinct;
        public List<Join> selectJoins;
        public PropertyInfo parentKey;
        public PropertyInfo childForeignKey;

        public IEnumerable<(LambdaExpression, object)> fieldsAndValues;

        public string GenerateSQL(SqlCommand cmd)
        {

            if (type == QueryType.SelectNormal)
            {

                string resultTable = Utilities.GetObjectName(resultType);

                string whereClause = where != null ? $"where {Utilities.Where(cmd, where, resultTable)}" : null;

                List<string> selectFields;
                if (this.selectFields.Count() == 0)
                {

                    var modelProps = modelType.GetProperties()
                        .Where(x => !Utilities.HasAttribute<Ignore>(x))
                        .Where(x => !selectJoins.Any(i => i.fieldToReceive == x));

                    selectFields = new List<string>((modelProps.Select(x => $"{resultTable}.{Utilities.GetObjectName(x)}")));

                }
                else
                    selectFields = new List<string>(this.selectFields.Select(x => $"{resultTable}.{Utilities.GetObjectName(Utilities.GetExpressionProperty(x))}"));

                string topClause = null;
                if (selectTop > 0)
                    topClause = $"top {selectTop}";

                string joinClause = null;
                if (selectJoins.Count() > 0)
                {

                    List<string> joinClauseList = new List<string>();

                    foreach (Join join in selectJoins)
                    {

                        string type = "join";
                        if (join.type == JoinType.Left)
                            type = "left join";
                        else if (join.type == JoinType.Right)
                            type = "right join";
                        else if (join.type == JoinType.FullOuter)
                            type = "full outer join";

                        string joinId = Utilities.GetObjectName(join.id);

                        joinClauseList.Add($"{type} {Utilities.GetObjectName(join.rightTable)} as {joinId} on {joinId}.{Utilities.GetObjectName(join.leftKey)} = {resultTable}.{Utilities.GetObjectName(join.rightKey)}");
                        selectFields.Add($"{joinId}.{Utilities.GetObjectName(join.fieldToSelect)} as {Utilities.GetObjectName(join.fieldToReceive)}");

                    }

                    joinClause = $"{string.Join(" ", joinClauseList)}";

                }

                string distinct = selectDistinct ? "distinct" : null;

                var parts = new[] {
                    "select",
                    distinct,
                    topClause,
                    string.Join(",", selectFields),
                    $"from {resultTable}",
                    joinClause,
                    whereClause
                }.Where(x => x != null);

                return string.Join(" ", parts);

            }
            else if (type == QueryType.SelectCascade)
            {

                //IEnumerable<string> resultTableVarFields = resultColumns.Select(x => $"{Utilities.GetObjectName(x)} {Utilities.TypeMap[x.PropertyType]}");
                //stepTableVars.Add($"declare {id} table({Utilities.Commaize(resultTableVarFields)});");

                return "";
            }
            else if (type == QueryType.Update)
            {

                string modelTable = Utilities.GetObjectName(modelType);
                string whereClause = where != null ? $"where {Utilities.Where(cmd, where, modelTable)}" : null;

                List<string> updateFieldsList = new List<string>();
                foreach (var field in fieldsAndValues)
                {

                    var param = cmd.Parameters.AddWithValue(Utilities.GetGuidString(), field.Item2);
                    updateFieldsList.Add($"{modelTable}.{Utilities.GetObjectName(Utilities.GetExpressionProperty(field.Item1))} = @{param.ParameterName}");

                }

                var parts = new[] {
                    "update",
                    modelTable,
                    "set",
                    $"{string.Join(",", updateFieldsList)}",
                    whereClause
                };

                return string.Join(" ", parts);

            }
            else if (type == QueryType.Delete)
            {
                string modelTable = Utilities.GetObjectName(modelType);
                string whereClause = where != null ? $"where {Utilities.Where(cmd, where, modelTable)}" : null;

                var parts = new[] {
                    "delete from",
                    modelTable,
                    whereClause
                };

                return string.Join(" ", parts);

            }
            else if (type == QueryType.Insert)
            {

                string modelTable = Utilities.GetObjectName(modelType);

                List<string> insertValues = new List<string>();
                foreach (var field in fieldsAndValues)
                {

                    var param = cmd.Parameters.AddWithValue(Utilities.GetGuidString(), field.Item2);
                    insertValues.Add($"@{param.ParameterName}");

                }

                var parts = new[] {
                    "insert into",
                    modelTable,
                    $"({string.Join(",", fieldsAndValues.Select(x => Utilities.GetObjectName(Utilities.GetExpressionProperty(x.Item1))))})",
                    "values",
                    $"({string.Join(",", insertValues)})"
                };

                return string.Join(" ", parts);

            }
            else
                throw new Exception("Invalid query type");

        }

    }

    public class Join
    {
        internal string id = Utilities.GetGuidString();
        internal Type rightTable;
        internal Type leftTable;
        internal PropertyInfo leftKey;
        internal PropertyInfo rightKey;
        internal PropertyInfo fieldToSelect;
        internal MemberInfo fieldToReceive;
        internal JoinType type;
    }

    public class CocoonORM
    {

        internal string connectionString;

        public CocoonORM(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public QueryBatch QueryBatch()
        {
            return new QueryBatch(this);
        }

        public static Join Join<LeftModelT, RightModelT>(Expression<Func<LeftModelT, object>> leftKey, Expression<Func<RightModelT, object>> rightKey, Expression<Func<RightModelT, object>> rightFieldToSelect, Expression<Func<LeftModelT, object>> leftFieldToReceive, JoinType type = JoinType.Left)
        {
            return new Join()
            {

                leftTable = typeof(LeftModelT),
                rightTable = typeof(RightModelT),
                leftKey = Utilities.GetExpressionProperty(leftKey),
                rightKey = Utilities.GetExpressionProperty(rightKey),
                fieldToSelect = Utilities.GetExpressionProperty(rightFieldToSelect),
                fieldToReceive = Utilities.GetExpressionProperty(leftFieldToReceive),
                type = type

            };
        }

        public IEnumerable<ModelT> Select<ModelT>(
            Expression<Func<ModelT, bool>> where = null,
            bool distinct = false,
            int top = 0,
            Join[] joins = null,
            params Expression<Func<ModelT, object>>[] fieldsToSelect)
        {

            return Select<ModelT, ModelT>(where, distinct, top, joins, fieldsToSelect);

        }

        public IEnumerable<ResultT> Select<ModelT, ResultT>(
            Expression<Func<ModelT, bool>> where = null,
            bool distinct = false,
            int top = 0,
            Join[] joins = null,
            params Expression<Func<ModelT, object>>[] fieldsToSelect)
        {

            var joinList = Utilities.GetJoins(typeof(ResultT), joins);

            var query = new Query
            {
                type = QueryType.SelectNormal,
                where = where,
                resultType = typeof(ResultT),
                modelType = typeof(ModelT),
                selectFields = fieldsToSelect,
                selectTop = top,
                selectDistinct = distinct,
                selectJoins = joinList
            };

            using (var connection = new SqlConnection(connectionString))
            using (var cmd = connection.CreateCommand())
            {

                cmd.CommandText = query.GenerateSQL(cmd);

                connection.Open();

                var list = new List<object>();

                using (SqlDataReader reader = cmd.ExecuteReader())
                    while (reader.Read())
                    {

                        object obj = Activator.CreateInstance(query.resultType);
                        Utilities.SetObjectFromReader(obj, reader);
                        list.Add(obj);

                    }

                return list.Cast<ResultT>();

            }

        }

        public int Update<ModelT>(Expression<Func<ModelT, bool>> where = null, params (Expression<Func<ModelT, object>>, object)[] fieldsToUpdate)
        {

            var query = new Query
            {
                type = QueryType.Update,
                where = where,
                modelType = typeof(ModelT),
                fieldsAndValues = fieldsToUpdate.Select(x => ((LambdaExpression)x.Item1, x.Item2))
            };

            using (var connection = new SqlConnection(connectionString))
            using (var cmd = connection.CreateCommand())
            {

                cmd.CommandText = query.GenerateSQL(cmd);

                connection.Open();
                return cmd.ExecuteNonQuery();

            }

        }

        public int Delete<ModelT>(Expression<Func<ModelT, bool>> where = null)
        {

            var query = new Query
            {
                type = QueryType.Delete,
                where = where,
                modelType = typeof(ModelT)
            };

            using (var connection = new SqlConnection(connectionString))
            using (var cmd = connection.CreateCommand())
            {

                cmd.CommandText = query.GenerateSQL(cmd);

                connection.Open();
                return cmd.ExecuteNonQuery();

            }

        }

        public ModelT Insert<ModelT>(params (Expression<Func<ModelT, object>>, object)[] fieldsToInsert)
        {

            var query = new Query
            {
                type = QueryType.Insert,
                modelType = typeof(ModelT),
                fieldsAndValues = fieldsToInsert.Select(x => ((LambdaExpression)x.Item1, x.Item2))
            };

            using (var connection = new SqlConnection(connectionString))
            using (var cmd = connection.CreateCommand())
            {

                cmd.CommandText = query.GenerateSQL(cmd);

                connection.Open();
                cmd.ExecuteNonQuery();

            }

            return default(ModelT);

        }

    }

    public class QueryBatch
    {

        private List<Query> queries = new List<Query>();
        private CocoonORM db;

        public QueryBatch(CocoonORM db)
        {
            this.db = db;
        }

        public void Select<ModelT>(
            List<ModelT> result,
            Expression<Func<ModelT, bool>> where = null,
            bool distinct = false,
            int top = 0,
            Join[] joins = null,
            params Expression<Func<ModelT, object>>[] fieldsToSelect)
        {
            Select<ModelT, ModelT>(result, where, distinct, top, joins, fieldsToSelect);
        }

        public void Select<ModelT, ResultT>(
            List<ModelT> result,
            Expression<Func<ModelT, bool>> where = null,
            bool distinct = false,
            int top = 0,
            Join[] joins = null,
            params Expression<Func<ModelT, object>>[] fieldsToSelect)
        {

            var joinList = Utilities.GetJoins(typeof(ResultT), joins);

            queries.Add(new Query
            {
                type = QueryType.SelectNormal,
                where = where,
                modelType = typeof(ModelT),
                resultType = typeof(ResultT),
                selectFields = fieldsToSelect,
                selectTop = top,
                selectDistinct = distinct,
                selectJoins = joinList
            });

        }

        public void Select<ParentT, ChildT>(
            Expression<Func<ParentT, object>> parentKey,
            Expression<Func<ChildT, object>> childForeignKey,
            Action<IEnumerable<ChildT>> onResult,
            Expression<Func<ParentT, ChildT, bool>> where = null,
            bool distinct = false,
            int top = 0,
            Join[] joins = null,
            params Expression<Func<ChildT, object>>[] fieldsToSelect)
        {
            Select<ParentT, ChildT, ChildT>(parentKey, childForeignKey, onResult, where, distinct, top, joins, fieldsToSelect);
        }

        public void Select<ParentT, ChildT, ResultT>(
            Expression<Func<ParentT, object>> parentKey,
            Expression<Func<ChildT, object>> childForeignKey,
            Action<IEnumerable<ResultT>> onResult,
            Expression<Func<ParentT, ChildT, bool>> where = null,
            bool distinct = false,
            int top = 0,
            Join[] joins = null,
            params Expression<Func<ChildT, object>>[] fieldsToSelect)
        {

            if (parentKey == null)
                throw new Exception("No parent key declared.");

            if (childForeignKey == null)
                throw new Exception("No child foreign key declared.");

            if (queries.Count() == 0 || !queries.Any(x => x.resultType == typeof(ParentT)))
                throw new Exception("No parent subset declared.");

            var joinList = Utilities.GetJoins(typeof(ResultT), joins);

            queries.Add(new Query
            {
                type = QueryType.SelectCascade,
                parentKey = Utilities.GetExpressionProperty(parentKey),
                childForeignKey = Utilities.GetExpressionProperty(childForeignKey),
                where = where,
                modelType = typeof(ChildT),
                resultType = typeof(ResultT),
                parentType = typeof(ParentT),
                selectFields = fieldsToSelect,
                selectTop = top,
                selectDistinct = distinct,
                selectJoins = joinList
            });

        }

        public void Execute()
        {

            if (queries.Count() == 0)
                throw new Exception("No queries to execute");

            using (var connection = new SqlConnection(db.connectionString))
            using (var cmd = connection.CreateCommand())
            {

                cmd.CommandText = string.Join("\r\n\r\n", queries.Select(x => x.GenerateSQL(cmd)));

                connection.Open();

                foreach (var query in queries)
                {

                    var list = new List<object>();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                        while (reader.Read())
                        {

                            object obj = Activator.CreateInstance(query.resultType);
                            Utilities.SetObjectFromReader(obj, reader);
                            list.Add(obj);

                        }

                }

            }

        }

    }

    public class Utilities
    {

        public static bool HasAttribute<T>(MemberInfo member)
        {

            return member.GetCustomAttributes(typeof(T), false).Length > 0;

        }

        public static bool HasAttribute<T>(Type property)
        {

            return property.GetCustomAttributes(typeof(T), false).Length > 0;

        }

        public static string GetName(MemberInfo member)
        {

            string name = member.Name;
            if (HasAttribute<OverrideName>(member))
                name = member.GetCustomAttribute<OverrideName>().name;

            return name;

        }

        public static string GetObjectName(MemberInfo member)
        {
            return GetObjectName(GetName(member));
        }

        public static string GetObjectName(string name)
        {
            return $"[{name}]";
        }

        public static string GetGuidString(Guid guid)
        {
            return guid.ToString("n");
        }

        public static string GetGuidString()
        {
            return GetGuidString(Guid.NewGuid());
        }

        public static IEnumerable<PropertyInfo> GetColumns(Type type)
        {
            return type.GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(x => !HasAttribute<Ignore>(x));
        }

        public static PropertyInfo GetExpressionProperty(LambdaExpression expression)
        {

            MemberExpression member;

            if (expression.Body.GetType() == typeof(UnaryExpression))
            {
                UnaryExpression unary = (UnaryExpression)expression.Body;
                member = (MemberExpression)unary.Operand;
            }
            else
                member = (MemberExpression)expression.Body;

            return (PropertyInfo)member.Member;

        }

        public static string Commaize(IEnumerable<object> values)
        {
            return string.Join(",", values);
        }

        public static object ChangeType(object value, Type conversionType)
        {

            if (value == null || value == DBNull.Value)
                if (conversionType.IsValueType)
                    return Activator.CreateInstance(conversionType);
                else
                    return null;

            if (conversionType.IsGenericType && conversionType.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
                conversionType = Nullable.GetUnderlyingType(conversionType);

            if (value.GetType() == conversionType)
                return value;

            try
            {
                return TypeDescriptor.GetConverter(conversionType).ConvertFrom(value);
            }
            catch
            {
                return Convert.ChangeType(value, conversionType);
            }

        }

        public static T ChangeType<T>(object value)
        {
            return (T)ChangeType(value, typeof(T));
        }

        public static object SetObjectFromReader(object objectToSet, SqlDataReader reader)
        {

            Type type = objectToSet.GetType();

            List<string> columns = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToList();

            foreach (PropertyInfo prop in type.GetProperties().Where(p => p.CanWrite))
            {

                string propName = GetName(prop);

                if (!columns.Contains(propName))
                    continue;

                object value = ChangeType(reader[propName], prop.PropertyType);
                prop.SetValue(objectToSet, value);

            }

            return objectToSet;

        }

        public static ExpandoObject SetExpandoObjectFromReader(SqlDataReader reader)
        {

            var expando = new ExpandoObject();
            var dictionary = (IDictionary<string, object>)expando;

            foreach (var field in Enumerable.Range(0, reader.FieldCount).Select(reader.GetName))
                dictionary.Add(field, reader[field]);

            return expando;

        }

        public static string Where(SqlCommand cmd, Expression where, string qualifier)
        {

            return new SQLExpressionTranslator(qualifier).GenerateSQLExpression(cmd, where);

        }

        public static List<Join> GetJoins(Type resultType, Join[] joins)
        {

            var joinList = new List<Join>();
            if (joins != null)
                joinList.AddRange(joins);

            foreach (var field in resultType.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                if (field.FieldType == typeof(Join[]))
                    joinList.AddRange((Join[])field.GetValue(null));
                else if (field.FieldType == typeof(Join))
                    joinList.Add((Join)field.GetValue(null));

            return joinList;

        }

        internal static readonly Dictionary<Type, string> TypeMap = new Dictionary<Type, string>
            {
                {typeof(Int64), "bigint"},
                {typeof(UInt64), "bigint"},

                {typeof(Byte[]), "varbinary"},
                {typeof(Boolean), "bit"},
                {typeof(String), "nvarchar(max)"},
                {typeof(Char), "nchar"},
                {typeof(DateTime), "datetime"},
                {typeof(DateTimeOffset), "datetimeoffset"},
                {typeof(Decimal), "decimal"},
                {typeof(Double), "float"},
                {typeof(Int32), "int"},
                {typeof(UInt32), "int"},
                {typeof(TimeSpan), "time"},
                {typeof(Guid), "uniqueidentifier"},
                {typeof(Int16), "smallint"},
                {typeof(UInt16), "smallint"},
                {typeof(Single), "real"},

                {typeof(Int64?), "bigint"},
                {typeof(UInt64?), "bigint"},
                {typeof(Byte?[]), "varbinary"},
                {typeof(Boolean?), "bit"},
                {typeof(Char?), "nchar"},
                {typeof(DateTime?), "date"},
                {typeof(DateTimeOffset?), "datetimeoffset"},
                {typeof(Decimal?), "decimal"},
                {typeof(Double?), "float"},
                {typeof(Int32?), "int"},
                {typeof(UInt32?), "int"},
                {typeof(TimeSpan?), "time"},
                {typeof(Guid?), "uniqueidentifier"},
                {typeof(Int16?), "smallint"},
                {typeof(UInt16?), "smallint"},
                {typeof(Single?), "real"}

            };


    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
    public class OverrideName : Attribute
    {

        internal string name;

        public OverrideName(string Name)
        {

            name = Name;

        }

    }

    [AttributeUsage(AttributeTargets.Property)]
    public class Ignore : Attribute { }

}
