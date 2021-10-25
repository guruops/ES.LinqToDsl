using System;

namespace GuruOps.ES.LinqToDsl.DAL.Utilities
{
    public static class DateTimeUtilities
    {
        /// <summary>
        /// Convert DateTime to Date string only using "format" string.
        /// </summary>
        /// <param name="dateTime">DateTime object</param>
        /// <param name="format">(optional) format, default is "MM-dd-yyyy"</param>
        /// <returns></returns>
        public static string ConvertToDateOnlyString(DateTime dateTime, string format = "MM-dd-yyyy")
        {
            if (string.IsNullOrWhiteSpace(format))
                return dateTime.ToLongDateString();

            return dateTime.ToString(format);
        }

        public static DateTime AddWorkdays(this DateTime date, int workDays)
        {
            DateTime tmpDate = date;
            while (workDays > 0)
            {
                tmpDate = tmpDate.AddDays(1);
                if (tmpDate.DayOfWeek < DayOfWeek.Saturday &&
                    tmpDate.DayOfWeek > DayOfWeek.Sunday)
                    workDays--;
            }

            return tmpDate;
        }

    }
}