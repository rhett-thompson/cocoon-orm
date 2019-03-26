using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Cocoon
{

    internal class SelectDef
    {
        public string id;
        public Type resultType;
        public Type parentType;
        public PropertyInfo parentKey;
        public PropertyInfo childForeignKey;
        public LambdaExpression where;
        public LambdaExpression[] fields;
    }
    
    public class CocoonQuery
    {

        private List<SelectDef> steps = new List<SelectDef>();

        public CocoonQuery Select<ModelT>(Expression<Func<ModelT, bool>> where = null, params Expression<Func<ModelT, object>>[] fieldsToSelect)
        {

            steps.Add(new SelectDef
            {
                where = where,
                id = $"@{Utilities.GetName(typeof(ModelT))}_{steps.Count}",
                resultType = typeof(ModelT),
                fields = fieldsToSelect
            });

            return this;
        }

        public CocoonQuery Select<ParentT, ChildT>(
            Expression<Func<ParentT, object>> parentKey,
            Expression<Func<ChildT, object>> childForeignKey,
            Expression<Func<ParentT, ChildT, bool>> where = null,
            params Expression<Func<ChildT, object>>[] fieldsToSelect)
        {

            if(parentKey == null)
                throw new Exception("No parent key declared.");

            if (childForeignKey == null)
                throw new Exception("No child foreign key declared.");

            if (steps.Count() == 0 || !steps.Any(x => x.resultType == typeof(ParentT)))
                throw new Exception("No parent subset declared.");

            steps.Add(new SelectDef
            {
                parentKey = Utilities.GetExpressionProperty(parentKey),
                childForeignKey = Utilities.GetExpressionProperty(childForeignKey),
                where = where,
                id = $"@{Utilities.GetName(typeof(ChildT))}_{steps.Count}",
                resultType = typeof(ChildT),
                parentType = typeof(ParentT),
                fields = fieldsToSelect
            });

            return this;
        }

        public List<IEnumerable<object>> List()
        {

            using (var connection = new SqlConnection("Server=tcp:teleportfile.database.windows.net,1433;Initial Catalog=teleportfile;Persist Security Info=False;User ID=teleportfile;Password=LordDagon1990;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"))
            using (var cmd = connection.CreateCommand())
            {

                if (steps.Count() == 1)
                {

                    var step = steps[0];
                    string resultTable = Utilities.GetObjectName(step.resultType);
                    string where = step.where != null ? $" where {Utilities.Where(cmd, step.where)}" : "";
                    string selectFields = step.fields.Count() == 0 ? "*" : Utilities.Commaize(step.fields.Select(x => Utilities.GetObjectName(Utilities.GetExpressionProperty(x))));

                    cmd.CommandText = $"select {selectFields} from {resultTable}{where};";

                }
                else if (steps.All(x => x.parentType == null))
                {

                    List<string> stepSQL = new List<string>();

                    foreach (var step in steps)
                    {

                        string resultTable = Utilities.GetObjectName(step.resultType);
                        string where = step.where != null ? $" where {Utilities.Where(cmd, step.where)}" : "";
                        string selectFields = step.fields.Count() == 0 ? "*" : Utilities.Commaize(step.fields.Select(x => Utilities.GetObjectName(Utilities.GetExpressionProperty(x))));

                        stepSQL.Add($"select {selectFields} from {resultTable}{where};");

                    }

                    cmd.CommandText = string.Join("\r\n", stepSQL);

                }
                else
                {

                    List<string> stepTableVars = new List<string>();
                    List<string> stepInserts = new List<string>();
                    List<string> stepSelects = new List<string>();

                    foreach (var step in steps)
                    {

                        var resultTable = Utilities.GetObjectName(step.resultType);
                        var resultColumns = step.fields.Count() == 0 ? Utilities.GetColumns(step.resultType) : step.fields.Select(x => Utilities.GetExpressionProperty(x));

                        //generate table var declarations
                        IEnumerable<string> resultTableVarFields = resultColumns.Select(x => $"{Utilities.GetObjectName(x)} {TypeMap[x.PropertyType]}");
                        stepTableVars.Add($"declare {step.id} table({Utilities.Commaize(resultTableVarFields)});");

                        //generate table var inserts
                        string where = step.where != null ? $" where {Utilities.Where(cmd, step.where)} " : "";

                        if (step.parentType != null)
                        {
                            var parent = steps.Take(steps.IndexOf(step)).Last(x => x.resultType == step.parentType);
                            stepInserts.Add($"insert into {step.id} select {Utilities.Commaize(resultColumns.Select(x => Utilities.GetObjectName(x)))} from {resultTable} where {Utilities.GetObjectName(step.childForeignKey)} in (select {Utilities.GetObjectName(step.parentKey)} from {parent.id});");
                        }
                        else
                            stepInserts.Add($"insert into {step.id} select {Utilities.Commaize(resultColumns.Select(x => Utilities.GetObjectName(x)))} from {resultTable}{where.TrimEnd()};");

                        //generate table var selects
                        stepSelects.Add($"select * from {step.id};");

                    }

                    cmd.CommandText = $"{string.Join("\r\n", stepTableVars)}\r\n\r\n{string.Join("\r\n", stepInserts)}\r\n\r\n{string.Join("\r\n", stepSelects)}";

                }

                var result = new List<IEnumerable<object>>();

                connection.Open();

                using (SqlDataReader reader = cmd.ExecuteReader())
                    foreach (var step in steps)
                    {
                        var list = new List<object>();

                        while (reader.Read())
                        {
                            object obj = Activator.CreateInstance(step.resultType);
                            Utilities.SetObjectFromReader(obj, reader);
                            list.Add(obj);
                        }

                        result.Add(list);

                        reader.NextResult();

                    }

                return result;

            }

        }

        public ResultT Single<ResultT>()
        {
            return (ResultT)List().Last().FirstOrDefault();
        }

        public IEnumerable<ValueT> Values<ValueT>(Expression<Func<ResultT, object>> field)
        {

            var results = List(field);
            return results.Select(x => Utilities.ChangeType<ValueT>(Utilities.GetExpressionProperty(field).GetValue(x)));

        }

        public ValueT Value<ValueT>(Expression<Func<ResultT, object>> field)
        {

            return Values<ValueT>(field).FirstOrDefault();

        }

        public int Delete()
        {
            return 0;
        }

        public int Update()
        {
            return 0;
        }

        public ResultT Insert(ResultT model)
        {
            return default(ResultT);
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

    public class CocoonResult
    {

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

        public static string Where(SqlCommand cmd, Expression where)
        {

            return new SQLExpressionTranslator().GenerateSQLExpression(cmd, where);

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
