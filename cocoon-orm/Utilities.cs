using System;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

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
        /// Generates a sequential COMB GUID.  It's based on the number of 10 nanosecond intervals that have elapsed since 1/1/1990 UTC.
        /// </summary>
        /// <returns></returns>
        public static Guid GenerateSequentialGuid()
        {

            byte[] guidArray = Guid.NewGuid().ToByteArray();

            DateTime now = DateTime.UtcNow;

            byte[] daysArray = BitConverter.GetBytes(new TimeSpan(now.Ticks - baseDate.Ticks).Days);
            byte[] msecsArray = BitConverter.GetBytes((long)(now.TimeOfDay.TotalMilliseconds / 3.333333));

            Array.Reverse(daysArray);
            Array.Reverse(msecsArray);

            Array.Copy(daysArray, daysArray.Length - 2, guidArray, guidArray.Length - 6, 2);
            Array.Copy(msecsArray, msecsArray.Length - 4, guidArray, guidArray.Length - 4, 4);

            return new Guid(guidArray);

        }

        /// <summary>
        /// Generates a sequential Base36 unique identifier.  It's based on the number of 10 nanosecond intervals that have elapsed since 1/1/1990 UTC.
        /// </summary>
        /// <returns></returns>
        public static string GenerateSequentialUID()
        {

            return Base36Encode(DateTime.UtcNow.Ticks);

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
        /// <param name="plainText"></param>
        /// <returns></returns>
        public static string Base64Encode(string plainText)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(plainText));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="base64EncodedData"></param>
        /// <returns></returns>
        public static string Base64Decode(string base64EncodedData)
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(base64EncodedData));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="plainText"></param>
        /// <returns></returns>
        public static string TripleDESEncrypt(string key, string plainText)
        {

            using (TripleDESCryptoServiceProvider tdes = new TripleDESCryptoServiceProvider())
            {

                tdes.Key = UTF8Encoding.UTF8.GetBytes(key);
                tdes.Mode = CipherMode.ECB;
                tdes.Padding = PaddingMode.PKCS7;

                byte[] toEncryptArray = UTF8Encoding.UTF8.GetBytes(plainText);
                byte[] resultArray = tdes.CreateEncryptor().TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);

                return Convert.ToBase64String(resultArray, 0, resultArray.Length);

            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="encryptedText"></param>
        /// <returns></returns>
        public static string TripleDESDecrypt(string key, string encryptedText)
        {

            using (TripleDESCryptoServiceProvider tdes = new TripleDESCryptoServiceProvider())
            {

                tdes.Key = UTF8Encoding.UTF8.GetBytes(key);
                tdes.Mode = CipherMode.ECB;
                tdes.Padding = PaddingMode.PKCS7;

                byte[] toEncryptArray = Convert.FromBase64String(encryptedText);
                byte[] resultArray = tdes.CreateDecryptor().TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);

                return UTF8Encoding.UTF8.GetString(resultArray);

            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string MD5Hash(string input)
        {

            MD5 md5 = MD5.Create();
            byte[] inputBytes = Encoding.ASCII.GetBytes(input);
            byte[] hash = md5.ComputeHash(inputBytes);

            StringBuilder sb = new StringBuilder();
            foreach (byte x in hash)
                sb.Append(x.ToString("X2"));

            return sb.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string SHA256Hash(string input)
        {

            byte[] bytes = Encoding.Unicode.GetBytes(input);
            SHA256Managed hashstring = new SHA256Managed();
            byte[] hash = hashstring.ComputeHash(bytes);

            StringBuilder sb = new StringBuilder();
            foreach (byte x in hash)
                sb.Append(x.ToString("X2"));

            return sb.ToString();
        }

    }
}
