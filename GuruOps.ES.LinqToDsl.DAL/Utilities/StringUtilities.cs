using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace GuruOps.ES.LinqToDsl.DAL.Utilities
{
    // Rename to StringUtilities once all methods has been moved here
    public static class StringUtilities
    {
        #region String Utilities

        /// <summary>
        /// Compresses the string.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns></returns>
        public static string CompressString(string text)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(text);
            MemoryStream memoryStream = new MemoryStream();
            using (GZipStream gZipStream = new GZipStream(memoryStream, CompressionMode.Compress, true))
            {
                gZipStream.Write(buffer, 0, buffer.Length);
            }

            memoryStream.Position = 0;

            byte[] compressedData = new byte[memoryStream.Length];
            memoryStream.Read(compressedData, 0, compressedData.Length);

            byte[] gZipBuffer = new byte[compressedData.Length + 4];
            Buffer.BlockCopy(compressedData, 0, gZipBuffer, 4, compressedData.Length);
            Buffer.BlockCopy(BitConverter.GetBytes(buffer.Length), 0, gZipBuffer, 0, 4);
            return Convert.ToBase64String(gZipBuffer);
        }

        /// <summary>
        /// Decompresses the string.
        /// </summary>
        /// <param name="compressedText">The compressed text.</param>
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
                {
                    gZipStream.Read(buffer, 0, buffer.Length);
                }

                return Encoding.UTF8.GetString(buffer);
            }
        }

        /// <summary>
        /// Split CSV to collection
        /// </summary>
        /// <param name="input">Input string to split</param>
        /// <param name="separator">Separator that delimits the substring, "," by default</param>
        /// <param name="options">StringSplitOptions, RemoveEmptyEntries by default</param>
        /// <param name="trimSpaces">Trim leading/trailing spaces, true by default</param>
        /// <returns></returns>
        public static List<string> Split(string input, string separator = ",", StringSplitOptions options = StringSplitOptions.RemoveEmptyEntries, bool trimSpaces = true)
        {
            input = input ?? "";
            if (trimSpaces)
                input = input.Trim();

            return input.Split(new string[] { separator }, options).Select(c => trimSpaces ? c.Trim() : c).ToList();
        }

        /// Change first character after dot to Lower case
        /// </summary>
        /// <param name="name">string to change</param>
        /// <returns>Updated string</returns>
        public static string FirstCharToLower(string name)
        {
            return name.First().ToString().ToLower() + string.Join("", name.Skip(1));
        }

        /// <summary>
        /// Change first character after dot to Upper case
        /// </summary>
        /// <param name="name">string to change</param>
        /// <returns>Updated string</returns>
        public static string FirstCharToUpper(string name)
        {
            return name.First().ToString().ToUpper() + string.Join("", name.Skip(1));
        }

        /// <summary>
        /// Return random string by specific length.
        /// </summary>
        /// <param name="length">String length.</param>
        /// <param name="numbersOnly">Numbers only flag.</param>
        /// <returns>String with specified length.</returns>
        public static string GenerateRandom(int length, bool numbersOnly = false, string allowedChars = null)
        {
            if (length < 0)
                return string.Empty;

            if (string.IsNullOrWhiteSpace(allowedChars))
                allowedChars = numbersOnly ? "0123456789" : "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

            const int byteSize = 0x100;
            char[] allowedCharSet = allowedChars.ToCharArray();
            if (byteSize < allowedCharSet.Length) throw new ArgumentException(
                $"allowedChars may contain no more than {byteSize} characters.");

            // Guid.NewGuid and System.Random are not particularly random. By using a
            // cryptographically-secure random number generator, the caller is always
            // protected, regardless of use.
            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                StringBuilder result = new StringBuilder();
                byte[] buf = new byte[128];
                while (result.Length < length)
                {
                    rng.GetBytes(buf);
                    for (int i = 0; i < buf.Length && result.Length < length; ++i)
                    {
                        // Divide the byte into allowedCharSet-sized groups. If the
                        // random value falls into the last group and the last group is
                        // too small to choose from the entire allowedCharSet, ignore
                        // the value in order to avoid biasing the result.
                        int outOfRangeStart = byteSize - (byteSize % allowedCharSet.Length);
                        if (outOfRangeStart <= buf[i]) continue;
                        result.Append(allowedCharSet[buf[i] % allowedCharSet.Length]);
                    }
                }
                return result.ToString();
            }
        }

        /// <summary>
        /// Convert number to string as ordinal number
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        public static string GetOrdinalNumberAsAString(int number)
        {
            return number + GetOrdinalNumberSuffix(number);
        }

        /// <summary>
        /// Convert a string as int into string as ordinal number
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        public static string GetOrdinalNumberAsAString(string number)
        {
            if (int.TryParse(number, out int num))
            {
                return GetOrdinalNumberAsAString(num);
            }

            return number;
        }

        /// <summary>
        /// Get ordinal number suffix
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        public static string GetOrdinalNumberSuffix(int number)
        {
            int j = number % 10, k = number % 100;

            if (j == 1 && k != 11)
                return "st";

            if (j == 2 && k != 12)
                return "nd";

            if (j == 3 && k != 13)
                return "rd";

            return "th";
        }

        /// <summary>
        /// Compare two string is a value was changed (returns true).
        /// Change from NULL to a value and vice versa means it was changed (returns true).
        /// Useful to compare if a reference to an entity Id changed or not.
        /// </summary>
        /// <param name="oldValue">Old string</param>
        /// <param name="newValue">New string</param>
        /// <param name="stringComparison">StringComparison, default is InvariantCultureIgnoreCase</param>
        /// <returns></returns>
        public static bool HasChanged(string oldValue, string newValue, StringComparison stringComparison = StringComparison.InvariantCultureIgnoreCase)
        {
            // Check if old Value was NULL
            if (string.IsNullOrWhiteSpace(oldValue))
            {
                if (string.IsNullOrWhiteSpace(newValue))
                    return false;

                return true;
            }

            // As old Value is NOT NULL, check newValue
            if (string.IsNullOrWhiteSpace(newValue))
                return true;

            return !oldValue.Equals(newValue, stringComparison);
        }

        public static List<string> ParseEmails(string value)
        {
            List<string> result = new List<string>();

            if (!string.IsNullOrWhiteSpace(value))
            {
                // Note: {1,} inside means it can have at least 1 subdomain 
                // (original regex was w/o this and did not work with couple subdomains e.g. staging.mailgun.com)
                string regex = @"([\w.-]+@([\w.-]+){1,}\.+\w{2,})";

                MatchCollection matches = new Regex(regex).Matches(value);

                foreach (Match m in matches)
                    result.Add(m.Value);
            }

            return result;
        }

        #endregion

        #region String Extensions
        /// <summary>
        /// Used ASCIIEncoding.Unicode encoding
        /// </summary>
        /// <returns></returns>
        public static int GetByteCount(this string str)
        {
            return ASCIIEncoding.Unicode.GetByteCount(str);
        }

        public static string RemoveBodyHtmlTag(this string html)
        {
            string theBody = string.Empty;
            if (!string.IsNullOrWhiteSpace(html))
            {
                RegexOptions options = RegexOptions.IgnoreCase | RegexOptions.Singleline;
                Regex regx = new Regex(@"<body(?:\s[^>]*)>(?<theBody>.*)</body>", options);

                Match match = regx.Match(html);
                if (match.Success)
                {
                    theBody = match.Groups["theBody"].Value;
                }
                else
                {
                    regx = new Regex(@"<body>(?<theBody>.*)</body>", options);
                    match = regx.Match(html);
                    theBody = string.Empty;
                    if (match.Success)
                        theBody = match.Groups["theBody"].Value;
                    else
                        theBody = html.Trim();
                }
            }
            return theBody.Trim();
        }

        public static string RemoveWhitespace(this string html)
        {
            string val = string.Empty;
            if (!string.IsNullOrWhiteSpace(html))
            {
                StringBuilder result =
                    new StringBuilder(Regex.Replace(html, @"\s*(<[^>]+>)\s*", "", RegexOptions.Singleline));
                result = result.Replace(Environment.NewLine, string.Empty);
                result = result.Replace(" ", string.Empty);
                result = result.Hacksomechar();
                val = result.ToString();
            }
            return val;
        }

        public static List<string> CSVToList(this string input, string separator = ",")
        {
            var result = new List<string>();
            if (!string.IsNullOrWhiteSpace(input))
            {
                result = input.Split(new string[] { separator }, StringSplitOptions.RemoveEmptyEntries).ToList();
            }

            return result;
        }

        public static Dictionary<string, string> CSVToDictionary(this string input, string csvSeparator = ",", string dictionarySeparator = "=")
        {
            var result = new Dictionary<string, string>();
            if (!string.IsNullOrWhiteSpace(input))
                result = input.CSVToList(csvSeparator).ListToDictionary(dictionarySeparator);

            return result;
        }

        public static KeyValuePair<string, string> ToKeyValuePair(this string input, string separator = "=")
        {
            var result = new KeyValuePair<string, string>();
            if (!string.IsNullOrWhiteSpace(input))
            {
                var items = input.Split(new string[] { separator }, StringSplitOptions.RemoveEmptyEntries);
                if (items.Length == 2)
                    result = new KeyValuePair<string, string>(items[0], items[1]);
            }

            return result;
        }

        public static byte[] ToByteArray(this string str)
        {
            var utf8 = new UTF8Encoding();
            return utf8.GetBytes(str);
        }
        #endregion

        #region Collection Utilities

        public static List<string> GetNew(List<string> newValues, List<string> currentValues)
        {
            var ids = newValues ?? new List<string>();
            currentValues = currentValues ?? new List<string>();

            ids.RemoveAll(c => currentValues.Contains(c));

            ids.RemoveDuplicateOrEmpty();
            return ids;
        }

        public static List<string> GetRemoved(List<string> newValues, List<string> currentValues)
        {
            var ids = currentValues ?? new List<string>();
            newValues = newValues ?? new List<string>();

            ids.RemoveAll(c => newValues.Contains(c));

            ids.RemoveDuplicateOrEmpty();
            return ids;
        }

        #endregion

        #region Collection Extensions

        public static int RemoveEmpty(this List<string> collection)
        {
            return collection.RemoveAll(c => string.IsNullOrWhiteSpace(c));
        }

        public static int RemoveDuplicateOrEmpty(this List<string> collection)
        {
            var count = collection.RemoveEmpty();
            var unique = collection.Select(c => c.Trim()).Distinct().ToList();
            count += collection.Count - unique.Count;
            foreach (var item in unique)
            {
                var index = collection.FindIndex(c => c.Trim() == item);
                collection.RemoveAll(c => c.Trim() == item);
                collection.Insert(index, item);
            }

            return count;
        }

        public static Dictionary<string, string> ListToDictionary(this List<string> collection, string separator = "=")
        {
            var result = new Dictionary<string, string>();
            foreach (var item in collection)
            {
                var pair = item.ToKeyValuePair(separator);
                if (!string.IsNullOrWhiteSpace(pair.Key))
                    result.Add(pair.Key, pair.Value);
            }

            return result;
        }

        public static bool Contains(this List<string> collection, List<string> input)
        {
            collection?.RemoveDuplicateOrEmpty();
            input?.RemoveDuplicateOrEmpty();
            return collection?.Any(c=> input?.Contains(c) == true) == true;
        }

        public static string ToCSV(this List<string> collection)
        {
            if (collection?.Any() == true)
                return string.Join(",", collection);

            return string.Empty;
        }
        #endregion
    }
}
