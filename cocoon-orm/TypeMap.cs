using System;
using System.Collections.Generic;

namespace Cocoon
{
    internal static class TypeMap
    {

        public static readonly Dictionary<string, Type> csMap = new Dictionary<string, Type>
        {

            {"bigint", typeof(Int64)}, 
            {"varbinary", typeof(Byte[])}, 
            {"bit", typeof(Boolean)}, 
            {"nvarchar(max)", typeof(String)}, 
            {"nchar", typeof(Char)}, 
            {"datetime", typeof(DateTime)}, 
            {"datetimeoffset", typeof(DateTimeOffset)}, 
            {"decimal", typeof(Decimal)}, 
            {"float", typeof(Double)}, 
            {"int", typeof(Int32)},
            {"time", typeof(TimeSpan)},
            {"uniqueidentifier", typeof(Guid)},
            {"smallint", typeof(Int16)},
            {"real", typeof(Single)}

        };

        public static readonly Dictionary<Type, string> csAlias = new Dictionary<Type, string>()
        {
            { typeof(byte), "byte" },
            { typeof(sbyte), "sbyte" },
            { typeof(short), "short" },
            { typeof(ushort), "ushort" },
            { typeof(int), "int" },
            { typeof(uint), "uint" },
            { typeof(long), "long" },
            { typeof(ulong), "ulong" },
            { typeof(float), "float" },
            { typeof(double), "double" },
            { typeof(decimal), "decimal" },
            { typeof(object), "object" },
            { typeof(bool), "bool" },
            { typeof(char), "char" },
            { typeof(string), "string" },
            { typeof(void), "void" }
        };

        public static readonly Dictionary<Type, string> sqlMap = new Dictionary<Type, string>
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
}
