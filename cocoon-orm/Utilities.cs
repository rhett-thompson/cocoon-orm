using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace Cocoon.ORM
{

    /// <summary>
    /// 
    /// </summary>
    public class Utilities
    {
        
        internal const string base36Digits = "0123456789abcdefghijklmnopqrstuvwxyz";
        internal static DateTime baseDate = new DateTime(1900, 1, 1);

        /// <summary>
        /// Changes the type of an object
        /// </summary>
        /// <param name="value"></param>
        /// <param name="conversionType"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Generates a sequential COMB GUID
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
        /// Generates a sequential Base36 unique identifier
        /// </summary>
        /// <returns></returns>
        public static string GenerateSequentialUID()
        {

            return Base36Encode(DateTime.Now.Ticks);

        }

        /// <summary>
        /// Decode Base36 string
        /// </summary>
        /// <param name="base36Encoded"></param>
        /// <returns></returns>
        public static long Base36Decode(string base36Encoded)
        {

            if (string.IsNullOrWhiteSpace(base36Encoded))
                throw new ArgumentException("Empty value.");

            base36Encoded = base36Encoded.ToUpper();

            bool negative = false;

            if (base36Encoded[0] == '-')
            {
                negative = true;
                base36Encoded = base36Encoded.Substring(1, base36Encoded.Length - 1);
            }

            if (base36Encoded.Any(c => !base36Digits.Contains(c)))
                throw new ArgumentException("Invalid value: \"" + base36Encoded + "\".");

            long decoded = 0L;

            for (var i = 0; i < base36Encoded.Length; ++i)
                decoded += base36Digits.IndexOf(base36Encoded[i]) * (long)BigInteger.Pow(base36Digits.Length, base36Encoded.Length - i - 1);

            return negative ? decoded * -1 : decoded;

        }

        /// <summary>
        /// Base36 encode a value
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
        /// Decode Base64 string
        /// </summary>
        /// <param name="base64Encoded"></param>
        /// <returns></returns>
        public static string Base64Decode(string base64Encoded)
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(base64Encoded));
        }

        /// <summary>
        /// Base64 encode a string
        /// </summary>
        /// <param name="plainText"></param>
        /// <returns></returns>
        public static string Base64Encode(string plainText)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(plainText));
        }

        /// <summary>
        /// Determines of the member has a custom attribute
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="member"></param>
        /// <returns></returns>
        public static bool HasAttribute<T>(MemberInfo member)
        {

            return member.GetCustomAttributes(typeof(T), false).Length > 0;

        }

        /// <summary>
        /// Determines of a class has a custom attribute
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="property"></param>
        /// <returns></returns>
        public static bool HasAttribute<T>(Type property)
        {

            return property.GetCustomAttributes(typeof(T), false).Length > 0;

        }

        /// <summary>
        /// Creates a list of scalars from a single field from a list of rows
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="rows"></param>
        /// <param name="fieldToMap"></param>
        /// <returns></returns>
        public static IEnumerable<T> FillScalarList<T>(IEnumerable<DataRow> rows, string fieldToMap = null)
        {

            List<T> list = new List<T>();
            foreach (DataRow row in rows)
            {
                if (fieldToMap == null)
                    list.Add((T)ChangeType(row[0], typeof(T)));
                else if (row.Table.Columns.Contains(fieldToMap))
                    list.Add((T)ChangeType(row[fieldToMap], typeof(T)));
            }
            return list;

        }

        /// <summary>
        /// Creates a list of scalars from a single field from a DataTable
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="table"></param>
        /// <param name="fieldToMap"></param>
        /// <returns></returns>
        public static IEnumerable<T> FillScalarList<T>(DataTable table, string fieldToMap = null)
        {

            return FillScalarList<T>(table.Select(), fieldToMap);

        }

        /// <summary>
        /// Fills a list from a list of rows
        /// </summary>
        /// <param name="type"></param>
        /// <param name="rows"></param>
        /// <returns></returns>
        public static IEnumerable<object> FillList(Type type, IEnumerable<DataRow> rows)
        {

            List<object> list = new List<object>();
            foreach (DataRow row in rows)
            {
                object obj = Activator.CreateInstance(type);
                SetFromRow(obj, row);
                list.Add(obj);
            }
            return list;

        }

        /// <summary>
        /// Fills a list from a DataTable
        /// </summary>
        /// <param name="type"></param>
        /// <param name="table"></param>
        /// <returns></returns>
        public static IEnumerable<object> FillList(Type type, DataTable table)
        {

            return FillList(type, table.Select());

        }

        /// <summary>
        /// Fills a list from a list of rows
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="rows"></param>
        /// <returns></returns>
        public static IEnumerable<T> FillList<T>(IEnumerable<DataRow> rows)
        {

            return FillList(typeof(T), rows).Cast<T>().ToList();

        }

        /// <summary>
        /// Fills a list from a DataTable
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="table"></param>
        /// <returns></returns>
        public static IEnumerable<T> FillList<T>(DataTable table)
        {

            return FillList(typeof(T), table.Select()).Cast<T>().ToList();

        }

        /// <summary>
        /// Sets a properties of an object from a DataRow
        /// </summary>
        /// <param name="objectToSet"></param>
        /// <param name="row"></param>
        public static void SetFromRow(object objectToSet, DataRow row)
        {

            Type type = objectToSet.GetType();

            foreach (PropertyInfo prop in type.GetProperties().Where(p => p.CanWrite))
            {

                string propName = CocoonORM.getName(prop);

                if (!row.Table.Columns.Contains(propName))
                    continue;

                DataColumn column = row.Table.Columns[propName];

                if (column == null)
                    continue;

                try
                {

                    object value = ChangeType(row[column], prop.PropertyType);
                    prop.SetValue(objectToSet, value);

                }
                catch
                {

                    throw new InvalidMemberException("Could not assign value", prop);

                }

            }

        }

        /// <summary>
        /// Sets the properties of an object from a DataReader
        /// </summary>
        /// <param name="objectToSet"></param>
        /// <param name="reader"></param>
        /// <param name="joins"></param>
        /// <returns></returns>
        public static object SetFromReader(object objectToSet, IDataReader reader, IEnumerable<JoinDef> joins)
        {

            Type type = objectToSet.GetType();

            List<string> columns = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToList();

            foreach (PropertyInfo prop in type.GetProperties().Where(p => p.CanWrite))
            {

                string propName;

                if (joins != null && joins.Any(j => j.FieldToRecieve == prop))
                    propName = prop.Name;
                else
                    propName = CocoonORM.getName(prop);

                if (!columns.Contains(propName))
                    continue;

                object value = ChangeType(reader[propName], prop.PropertyType);
                prop.SetValue(objectToSet, value);


            }

            return objectToSet;

        }

        /// <summary>
        /// Creates an SHA256 hash of a string
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string SHA256(string value)
        {
            using (SHA256 hash = System.Security.Cryptography.SHA256.Create())
            {
                return string.Join("", hash
                  .ComputeHash(Encoding.UTF8.GetBytes(value))
                  .Select(item => item.ToString("x2")));
            }

        }

        /// <summary>
        /// Creates an MD5 hash of a string
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string MD5(string value)
        {

            using (MD5 hash = System.Security.Cryptography.MD5.Create())
            {
                return string.Join("", hash
                  .ComputeHash(Encoding.GetEncoding(1252).GetBytes(value))
                  .Select(item => item.ToString("x2")));
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static string MD5ListHash<T>(IEnumerable<T> list)
        {

            Type type = typeof(T);
            IEnumerable<PropertyInfo> props = type.GetProperties().Where(p => HasAttribute<Column>(p));

            List<string> rows = new List<string>();
            foreach (T item in list)
            {

                List<object> values = new List<object>();
                foreach (PropertyInfo prop in props)
                {
                    object v = prop.GetValue(item);
                    if (v != null && (prop.PropertyType == typeof(DateTime) || prop.PropertyType == typeof(DateTime?)))
                        values.Add(((DateTime)v).ToString("MM/dd/yyyy H:mm:ss"));
                    else
                        values.Add(v);
                }
                rows.Add(MD5(string.Join(",", values)).ToUpper());

            }

            string joined = string.Join(",", rows);
            return MD5(joined).ToUpper();

        }

        /// <summary>
        /// Compresses a string using GZip
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string CompressString(string text)
        {

            byte[] buffer = Encoding.UTF8.GetBytes(text);
            MemoryStream memoryStream = new MemoryStream();
            using (GZipStream gZipStream = new GZipStream(memoryStream, CompressionMode.Compress, true))
                gZipStream.Write(buffer, 0, buffer.Length);

            memoryStream.Position = 0;

            byte[] compressedData = new byte[memoryStream.Length];
            memoryStream.Read(compressedData, 0, compressedData.Length);

            byte[] gZipBuffer = new byte[compressedData.Length + 4];
            Buffer.BlockCopy(compressedData, 0, gZipBuffer, 4, compressedData.Length);
            Buffer.BlockCopy(BitConverter.GetBytes(buffer.Length), 0, gZipBuffer, 0, 4);
            return Convert.ToBase64String(gZipBuffer);

        }

        /// <summary>
        /// Decompresses a string using GZip
        /// </summary>
        /// <param name="compressedText"></param>
        /// <returns></returns>
        public static string DecompressString(string compressedText)
        {
            byte[] gZipBuffer = Convert.FromBase64String(compressedText);
            using (MemoryStream memoryStream = new MemoryStream())
            {
                int dataLength = BitConverter.ToInt32(gZipBuffer, 0);
                memoryStream.Write(gZipBuffer, 4, gZipBuffer.Length - 4);

                byte[] buffer = new byte[dataLength];

                memoryStream.Position = 0;
                using (GZipStream gZipStream = new GZipStream(memoryStream, CompressionMode.Decompress))
                    gZipStream.Read(buffer, 0, buffer.Length);

                return Encoding.UTF8.GetString(buffer);
            }
        }
        
    }
}
