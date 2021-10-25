using System.Collections.Generic;

namespace GuruOps.ES.LinqToDsl.DAL.Utilities
{
    public static class DictionaryUtilities
    {
        #region Extension

        public static T GetValue<T>(this Dictionary<string, T> dictionary, string key)
        {
            T value = default(T);
            if (dictionary != null && !string.IsNullOrWhiteSpace(key))
            {
                if (!dictionary.TryGetValue(key, out value))
                    dictionary.TryGetValue(key.Trim().ToLower(), out value);
            }

            return value;
        }

        #endregion
    }
}
