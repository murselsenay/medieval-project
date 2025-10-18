using System;
using System.Globalization;

namespace Utilities.Extensions
{
    public static class DateTimeExtensions
    {
        public static long UnixTime
        {
            get { return (long)(DateTime.Now.Subtract(new DateTime(1970, 1, 1))).TotalSeconds; }
        }

        public static long ToUnixTime(this DateTime date)
        {
            return (long)(date.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }

        public static DateTime FromUnixTime(long time)
        {
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(time);
            return dtDateTime;
        }

        public static int GetWeekIndex(this DateTime dateTime)
        {
            CultureInfo cul = CultureInfo.CurrentCulture;

            return cul.Calendar.GetWeekOfYear(
                dateTime,
                CalendarWeekRule.FirstDay,
                DayOfWeek.Monday);
        }

        #region TimePass

        public static bool CheckTimePassedDaily(DateTime currentDay, DateTime savedDay, int hour = 14)
        {
            if (currentDay.Year > savedDay.Year)
            {
                return true;
            }
            else
            {
                if (currentDay.DayOfYear <= savedDay.DayOfYear)
                {
                    return false;
                }
            }

            if ((currentDay.DayOfYear - savedDay.DayOfYear) > 1)
            {
                return true;
            }
            if (currentDay.Hour >= hour)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool CheckTimePassedWeekly(DateTime currentDay, DateTime savedDay, int hour = 14)
        {
            if (currentDay.Year > savedDay.Year)
            {
                return true;
            }
            else
            {
                if (currentDay.GetWeekIndex() <= savedDay.GetWeekIndex())
                {
                    return false;
                }
            }

            if ((currentDay.GetWeekIndex() - savedDay.GetWeekIndex()) > 1)
            {
                return true;
            }
            if (currentDay.GetWeekIndex() > savedDay.GetWeekIndex())
            {
                if (currentDay.Day == 0)
                {
                    if (currentDay.Hour >= 14)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return false;
            }
        }


        #endregion
    }
}
