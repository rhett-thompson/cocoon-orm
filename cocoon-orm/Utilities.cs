﻿using Cocoon.Annotations;
using System;
using System.Linq;
using System.Numerics;
using System.Reflection;

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
                overrideName = annotation.overrideName;

            }
            else
            {

                Column annotation = member.GetCustomAttribute<Column>(false);
                if (annotation != null)
                    overrideName = annotation.overrideName;

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

    }
}
