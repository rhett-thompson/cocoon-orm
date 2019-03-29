using System;
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
        Select,
        Update,
        Insert
    }

    public enum JoinType
    {

        /// <summary>
        /// Left join
        /// </summary>
        Left,

        /// <summary>
        /// Inner join
        /// </summary>
        Inner,

        /// <summary>
        /// Right join
        /// </summary>
        Right,

        /// <summary>
        /// Full outer join
        /// </summary>
        FullOuter

    }

    internal class Query
    {

        public QueryType type;
        public string id;
        public Type modelType;
        public Type resultType;
        public Type parentType;
        public PropertyInfo parentKey;
        public PropertyInfo childForeignKey;
        public LambdaExpression where;
        public LambdaExpression[] fields;
        public Action<List<object>> onResult;

        public int selectTop;
        public bool selectDistinct;
        public List<Join> selectJoins;

        public string GenerateSQL(SqlCommand cmd)
        {

            string resultTable = Utilities.GetObjectName(modelType);
            string whereClause = where != null ? $" where {Utilities.Where(cmd, where, resultTable)}" : "";

            List<string> selectFields;
            if (fields.Count() == 0)
            {

                var modelProps = modelType.GetProperties()
                    .Where(x => !Utilities.HasAttribute<Ignore>(x))
                    .Where(x => !selectJoins.Any(i => i.fieldToReceive == x));

                selectFields = new List<string>((modelProps.Select(x => $"{resultTable}.{Utilities.GetObjectName(x)}")));

            }
            else
                selectFields = new List<string>(fields.Select(x => $"{resultTable}.{Utilities.GetObjectName(Utilities.GetExpressionProperty(x))}"));

            string topClause = "";
            if (selectTop > 0)
                topClause = $"top {selectTop}";

            string joinClause = null;
            if (selectJoins.Count() > 0)
            {

                List<string> joinClauseList = new List<string>();

                foreach (Join join in selectJoins)
                {

                    string joinPart = "join";
                    if (join.type == JoinType.Left)
                        joinPart = "left join";
                    else if (join.type == JoinType.Right)
                        joinPart = "right join";
                    else if (join.type == JoinType.FullOuter)
                        joinPart = "full outer join";

                    string alias = Utilities.GetObjectName($"join_{selectJoins.IndexOf(join)}");
                    joinClauseList.Add($"{joinPart} {Utilities.GetObjectName(join.rightTable)} as {alias} on {resultTable}.{Utilities.GetObjectName(join.leftKey)} = {alias}.{Utilities.GetObjectName(join.rightKey)}");
                    selectFields.Add($"{alias}.{Utilities.GetObjectName(join.fieldToSelect)} as {Utilities.GetObjectName(join.fieldToReceive)}");

                }

                joinClause = string.Join("\r\n", joinClauseList);

            }

            return $"select {(selectDistinct ? "distinct" : "")} {topClause} {string.Join(",", selectFields)} from {resultTable}{joinClause}{whereClause};";

        }

    }

    public class Join
    {
        internal Guid id = Guid.NewGuid();
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

        public async Task<IEnumerable<ModelT>> Select<ModelT>(
            Expression<Func<ModelT, bool>> where = null,
            bool distinct = false,
            int top = 0,
            Join[] joins = null,
            params Expression<Func<ModelT, object>>[] fieldsToSelect)
        {

            return await Select<ModelT, ModelT>(where, distinct, top, joins, fieldsToSelect);

        }

        public async Task<IEnumerable<ResultT>> Select<ModelT, ResultT>(
            Expression<Func<ModelT, bool>> where = null,
            bool distinct = false,
            int top = 0,
            Join[] joins = null,
            params Expression<Func<ModelT, object>>[] fieldsToSelect)
        {

            var joinList = new List<Join>();
            if (joins != null)
                joinList.AddRange(joins);

            foreach (var field in typeof(ResultT).GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {

                if (joinList == null)
                    joinList = new List<Join>();

                if (field.FieldType == typeof(Join[]))
                    joinList.AddRange((Join[])field.GetValue(null));
                else if (field.FieldType == typeof(Join))
                    joinList.Add((Join)field.GetValue(null));
            }

            var query = new Query
            {
                type = QueryType.Select,
                where = where,
                resultType = typeof(ResultT),
                modelType = typeof(ModelT),
                fields = fieldsToSelect,
                selectTop = top,
                selectDistinct = distinct,
                selectJoins = joinList
            };

            using (var connection = new SqlConnection(connectionString))
            using (var cmd = connection.CreateCommand())
            {

                cmd.CommandText = query.GenerateSQL(cmd);

                await connection.OpenAsync();

                var list = new List<object>();

                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    while (reader.Read())
                    {

                        object obj = Activator.CreateInstance(query.resultType);
                        Utilities.SetObjectFromReader(obj, reader);
                        list.Add(obj);

                    }

                return list.Cast<ResultT>();

            }



        }

        //public List<IEnumerable<object>> List()
        //{

        //    using (var connection = new SqlConnection("Server=tcp:teleportfile.database.windows.net,1433;Initial Catalog=teleportfile;Persist Security Info=False;User ID=teleportfile;Password=LordDagon1990;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"))
        //    using (var cmd = connection.CreateCommand())
        //    {

        //        if (steps.Count() == 1)
        //        {

        //            var step = steps[0];
        //            string resultTable = Utilities.GetObjectName(step.resultType);
        //            string where = step.where != null ? $" where {Utilities.Where(cmd, step.where)}" : "";
        //            string selectFields = step.fields.Count() == 0 ? "*" : Utilities.Commaize(step.fields.Select(x => Utilities.GetObjectName(Utilities.GetExpressionProperty(x))));

        //            cmd.CommandText = $"select {selectFields} from {resultTable}{where};";

        //        }
        //        else if (steps.All(x => x.parentType == null))
        //        {

        //            List<string> stepSQL = new List<string>();

        //            foreach (var step in steps)
        //            {

        //                string resultTable = Utilities.GetObjectName(step.resultType);
        //                string where = step.where != null ? $" where {Utilities.Where(cmd, step.where)}" : "";
        //                string selectFields = step.fields.Count() == 0 ? "*" : Utilities.Commaize(step.fields.Select(x => Utilities.GetObjectName(Utilities.GetExpressionProperty(x))));

        //                stepSQL.Add($"select {selectFields} from {resultTable}{where};");

        //            }

        //            cmd.CommandText = string.Join("\r\n", stepSQL);

        //        }
        //        else
        //        {

        //            List<string> stepTableVars = new List<string>();
        //            List<string> stepInserts = new List<string>();
        //            List<string> stepSelects = new List<string>();

        //            foreach (var step in steps)
        //            {

        //                var resultTable = Utilities.GetObjectName(step.resultType);
        //                var resultColumns = step.fields.Count() == 0 ? Utilities.GetColumns(step.resultType) : step.fields.Select(x => Utilities.GetExpressionProperty(x));

        //                //generate table var declarations
        //                IEnumerable<string> resultTableVarFields = resultColumns.Select(x => $"{Utilities.GetObjectName(x)} {TypeMap[x.PropertyType]}");
        //                stepTableVars.Add($"declare {step.id} table({Utilities.Commaize(resultTableVarFields)});");

        //                //generate table var inserts
        //                string where = step.where != null ? $" where {Utilities.Where(cmd, step.where)} " : "";

        //                if (step.parentType != null)
        //                {
        //                    var parent = steps.Take(steps.IndexOf(step)).Last(x => x.resultType == step.parentType);
        //                    stepInserts.Add($"insert into {step.id} select {Utilities.Commaize(resultColumns.Select(x => Utilities.GetObjectName(x)))} from {resultTable} where {Utilities.GetObjectName(step.childForeignKey)} in (select {Utilities.GetObjectName(step.parentKey)} from {parent.id});");
        //                }
        //                else
        //                    stepInserts.Add($"insert into {step.id} select {Utilities.Commaize(resultColumns.Select(x => Utilities.GetObjectName(x)))} from {resultTable}{where.TrimEnd()};");

        //                //generate table var selects
        //                stepSelects.Add($"select * from {step.id};");

        //            }

        //            cmd.CommandText = $"{string.Join("\r\n", stepTableVars)}\r\n\r\n{string.Join("\r\n", stepInserts)}\r\n\r\n{string.Join("\r\n", stepSelects)}";

        //        }

        //        var result = new List<IEnumerable<object>>();

        //        connection.Open();

        //        using (SqlDataReader reader = cmd.ExecuteReader())
        //            foreach (var step in steps)
        //            {
        //                var list = new List<object>();

        //                while (reader.Read())
        //                {
        //                    object obj = Activator.CreateInstance(step.resultType);
        //                    Utilities.SetObjectFromReader(obj, reader);
        //                    list.Add(obj);
        //                }

        //                result.Add(list);

        //                reader.NextResult();

        //            }

        //        return result;

        //    }

        //}

        //public int Delete()
        //{
        //    return 0;
        //}

        //public int Update()
        //{
        //    return 0;
        //}

        //public ResultT Insert(ResultT model)
        //{
        //    return default(ResultT);
        //}

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

    public class QueryBatch
    {

        private List<Query> queries = new List<Query>();
        private CocoonORM db;

        public QueryBatch(CocoonORM db)
        {
            this.db = db;
        }

        public void Select<ModelT>(
            Action<IEnumerable<ModelT>> onResult,
            Expression<Func<ModelT, bool>> where = null,
            bool distinct = false,
            int top = 0,
            Join[] joins = null,
            params Expression<Func<ModelT, object>>[] fieldsToSelect)
        {
            Select<ModelT, ModelT>(onResult, where, distinct, top, joins, fieldsToSelect);
        }

        public void Select<ModelT, ResultT>(
            Action<IEnumerable<ResultT>> onResult,
            Expression<Func<ModelT, bool>> where = null,
            bool distinct = false,
            int top = 0,
            Join[] joins = null,
            params Expression<Func<ModelT, object>>[] fieldsToSelect)
        {

            if (onResult == null)
                throw new Exception("OnResult action required");

            Action<List<object>> onResultWrapper = (list) => { onResult(list.Cast<ResultT>()); };

            var joinList = new List<Join>();

            foreach (var field in typeof(ResultT).GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {

                if (joinList == null)
                    joinList = new List<Join>();

                if (field.FieldType == typeof(Join[]))
                    joinList.AddRange((Join[])field.GetValue(null));
                else if (field.FieldType == typeof(Join))
                    joinList.Add((Join)field.GetValue(null));
            }

            queries.Add(new Query
            {
                type = QueryType.Select,
                where = where,
                id = $"@{Utilities.GetName(typeof(ModelT))}_{queries.Count}",
                modelType = typeof(ModelT),
                resultType = typeof(ResultT),
                fields = fieldsToSelect,
                onResult = onResultWrapper,
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

            Action<List<object>> fillAction = (list) => { onResult(list.Cast<ResultT>()); };

            var joinList = new List<Join>();

            foreach (var field in typeof(ResultT).GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {

                if (joinList == null)
                    joinList = new List<Join>();

                if (field.FieldType == typeof(Join[]))
                    joinList.AddRange((Join[])field.GetValue(null));
                else if (field.FieldType == typeof(Join))
                    joinList.Add((Join)field.GetValue(null));
            }

            queries.Add(new Query
            {
                type = QueryType.Select,
                parentKey = Utilities.GetExpressionProperty(parentKey),
                childForeignKey = Utilities.GetExpressionProperty(childForeignKey),
                where = where,
                id = $"@{Utilities.GetName(typeof(ChildT))}_{queries.Count}",
                modelType = typeof(ChildT),
                resultType = typeof(ResultT),
                parentType = typeof(ParentT),
                fields = fieldsToSelect,
                onResult = fillAction,
                selectTop = top,
                selectDistinct = distinct,
                selectJoins = joinList
            });

        }

        public async void Execute()
        {

            if (queries.Count() == 0)
                throw new Exception("No queries to execute");

            using (var connection = new SqlConnection(db.connectionString))
            using (var cmd = connection.CreateCommand())
            {

                cmd.CommandText = string.Join("\r\n", queries.Select(x => x.GenerateSQL(cmd)));

                await connection.OpenAsync();

                //var list = new List<object>();

                //using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                //    while (reader.Read())
                //    {

                //        object obj = Activator.CreateInstance(query.resultType);
                //        Utilities.SetObjectFromReader(obj, reader);
                //        list.Add(obj);

                //    }

            }

            foreach (var query in queries)
            {

                //var r = new List<object>();
                //r.Add(Activator.CreateInstance(query.resultType));
                //r.Add(Activator.CreateInstance(query.resultType));
                //r.Add(Activator.CreateInstance(query.resultType));

                //query.fillAction(r);

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
