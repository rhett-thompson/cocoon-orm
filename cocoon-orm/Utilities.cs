using Cocoon.Annotations;
using System;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Collections.Generic;
using System.Data;
using System.ComponentModel;

namespace Cocoon
{

    /// <summary>
    /// 
    /// </summary>
    public class Utilities
    {

        private const string base36Digits = "0123456789abcdefghijklmnopqrstuvwxyz";
        private static DateTime baseDate = new DateTime(1900, 1, 1);

        /// <summary>
        /// Changes the type of an object
        /// </summary>
        /// <param name="value"></param>
        /// <param name="conversionType"></param>
        /// <returns></returns>
        public static object ChangeType(object value, Type conversionType)
        {

            if (value == null)
                if (conversionType.IsValueType)
                    return Activator.CreateInstance(conversionType);
                else
                    return null;

            if (value.GetType() == conversionType)
                return value;

            if (conversionType.IsGenericType && conversionType.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
                conversionType = Nullable.GetUnderlyingType(conversionType);

            try
            {
                return TypeDescriptor.GetConverter(conversionType).ConvertFrom(value);
            }
            catch
            {
                return Convert.ChangeType(value, conversionType);

            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static Guid GenerateSequentialGuid()
        {

            byte[] guidArray = Guid.NewGuid().ToByteArray();

            DateTime now = DateTime.Now;

            TimeSpan days = new TimeSpan(now.Ticks - baseDate.Ticks);
            TimeSpan msecs = now.TimeOfDay;

            byte[] daysArray = BitConverter.GetBytes(days.Days);
            byte[] msecsArray = BitConverter.GetBytes((long)(msecs.TotalMilliseconds / 3.333333));

            Array.Reverse(daysArray);
            Array.Reverse(msecsArray);

            Array.Copy(daysArray, daysArray.Length - 2, guidArray, guidArray.Length - 6, 2);
            Array.Copy(msecsArray, msecsArray.Length - 4, guidArray, guidArray.Length - 4, 4);

            return new Guid(guidArray);

        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static string GenerateSequentialUID()
        {

            return Base36Encode(DateTime.Now.Ticks);

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static long Base36Decode(string value)
        {

            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Empty value.");

            value = value.ToUpper();

            bool negative = false;

            if (value[0] == '-')
            {
                negative = true;
                value = value.Substring(1, value.Length - 1);
            }

            if (value.Any(c => !base36Digits.Contains(c)))
                throw new ArgumentException("Invalid value: \"" + value + "\".");

            long decoded = 0L;

            for (var i = 0; i < value.Length; ++i)
                decoded += base36Digits.IndexOf(value[i]) * (long)BigInteger.Pow(base36Digits.Length, value.Length - i - 1);

            return negative ? decoded * -1 : decoded;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string Base36Encode(long value)
        {
            if (value == long.MinValue)
                return "-1Y2P0IJ32E8E8";

            bool negative = value < 0;

            value = Math.Abs(value);

            string encoded = string.Empty;

            do
                encoded = base36Digits[(int)(value % base36Digits.Length)] + encoded;

            while ((value /= base36Digits.Length) != 0);

            return negative ? "-" + encoded : encoded;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="member"></param>
        /// <returns></returns>
        public static bool HasAttribute<T>(MemberInfo member)
        {

            return member.GetCustomAttributes(typeof(T), false).Length > 0;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="property"></param>
        /// <returns></returns>
        public static bool HasAttribute<T>(Type property)
        {

            return property.GetCustomAttributes(typeof(T), false).Length > 0;

        }

        /// <summary>
        /// Returns the name of a column
        /// </summary>
        /// <param name="member"></param>
        /// <returns></returns>
        public static string GetColumnName(MemberInfo member)
        {

            string name = null;
            string overrideName = null;

            if (HasAttribute<ForeignColumn>(member))
            {

                ForeignColumn annotation = member.GetCustomAttribute<ForeignColumn>(false);
                overrideName = annotation.OverrideName;

            }
            else
            {

                Column annotation = member.GetCustomAttribute<Column>(false);
                if (annotation != null)
                    overrideName = annotation.OverrideName;

            }

            if (!string.IsNullOrEmpty(overrideName))
                name = overrideName;
            else
                name = member.Name;

            return name;

        }

        /// <summary>
        /// Returns the name of a table
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string GetTableName(Type type)
        {

            string name;
            if (HasAttribute<Table>(type))
            {

                Table annotation = type.GetCustomAttribute<Table>(false);

                if (annotation.tableName == null)
                    name = type.Name;
                else
                    name = annotation.tableName;

            }
            else
                name = type.Name;

            return name;

        }

        /// <summary>
        /// Fills a list from a DataTable
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="table"></param>
        /// <param name="fieldToMap"></param>
        /// <returns></returns>
        public static List<T> FillScalarList<T>(DataTable table, string fieldToMap = null)
        {

            List<T> list = new List<T>();
            foreach (DataRow row in table.Rows)
            {
                if (fieldToMap == null)
                    list.Add((T)ChangeType(row[0], typeof(T)));
                else if (row.Table.Columns.Contains(fieldToMap))
                    list.Add((T)ChangeType(row[fieldToMap], typeof(T)));
            }
            return list;

        }

        /// <summary>
        /// Fills a list of objects from the rows of a DataTable
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="table"></param>
        /// <returns></returns>
        public static List<T> FillList<T>(DataTable table)
        {

            List<T> list = new List<T>();
            foreach (DataRow row in table.Rows)
            {
                T obj = (T)Activator.CreateInstance(typeof(T));
                SetFromRow(obj, row);
                list.Add(obj);
            }
            return list;

        }

        /// <summary>
        /// Sets an objects properties from a DataRow
        /// </summary>
        /// <param name="objectToSet"></param>
        /// <param name="row"></param>
        public static void SetFromRow(object objectToSet, DataRow row)
        {

            Type type = objectToSet.GetType();

            PropertyInfo[] propertiesToSet = type.GetProperties();
            foreach (PropertyInfo prop in propertiesToSet)
            {

                string propName;

                if (HasAttribute<ForeignColumn>(prop))
                    propName = prop.Name;
                else
                    propName = GetColumnName(prop);

                if (!row.Table.Columns.Contains(propName))
                    continue;

                DataColumn column = row.Table.Columns[propName];

                if (column == null)
                    continue;

                try
                {

                    if (row[column] == DBNull.Value)
                        prop.SetValue(objectToSet, null);
                    else
                        prop.SetValue(objectToSet, row[column]);

                }
                catch
                {

                    throw new Exception(string.Format("Could not assign value to '{0}'.", propName));

                }

            }

        }
        
    }
}
