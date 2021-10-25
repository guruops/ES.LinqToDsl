using System;
using System.Globalization;

namespace GuruOps.ES.LinqToDsl.DAL.Utilities
{
    public static class Converter
    {
        /// <summary>
        /// Returns an object of specified type
        /// </summary>
        /// <typeparam name="T">Type of object to return</typeparam>
        /// <param name="value">An object to convert</param>
        /// <param name="defaultValue">Default value to return if input value is null</param>
        /// <returns></returns>
        public static T ConvertTo<T>(object value, T defaultValue = default(T))
        {
            T result = defaultValue;

            if (value != null)
            {
                if (value is T)
                    result = (T)value;
                else
                {
                    try
                    {
                        return (T)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
                    }
                    catch
                    {
                        return defaultValue;
                    }
                }
            }

            return result;
        }
    }
}
