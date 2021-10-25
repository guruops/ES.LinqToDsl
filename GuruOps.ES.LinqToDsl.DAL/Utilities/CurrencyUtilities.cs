using System.Globalization;

namespace GuruOps.ES.LinqToDsl.DAL.Utilities
{
    public static class CurrencyUtilities
    {
        private static string DefaultCulture = "en-US";

        public static string DecimalToCurrencyString(decimal value, string culture = "en-US")
        {
            if (string.IsNullOrWhiteSpace(culture))
                culture = DefaultCulture;

            CultureInfo cultureInfo = CultureInfo.GetCultureInfo(culture);

            if (cultureInfo == null)
            {
                cultureInfo = new CultureInfo(DefaultCulture);
            }

            return value.ToString("c", cultureInfo);
        }
    }
}