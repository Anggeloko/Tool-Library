﻿using System;
using System.Collections.Generic;
using System.Globalization;

namespace Axl.Base.Statics
{
    public static class DateTimeExtensions
    {
        public static string ToFullDateTime(this DateTime e) => e.ToString("yyyy-MM-dd HH:mm:ss.fff");
        public static string ToDateOnly(this DateTime e) => e.ToString("yyyy-MM-dd");
        public static string ToMinutePrecision(this DateTime e) => e.ToString("yyyy-MM-dd HH:mm:00.000");
        public static string ToHourPrecision(this DateTime e) => e.ToString("yyyy-MM-dd HH:00:00.000");
        public static string ToLongTime(this DateTime e) => e.ToString("HH:mm:ss.fff");
        public static string ToCompactDate(this DateTime e) => e.ToString("yyyyMMdd");
        public static string ToTimestamp(this DateTime e) => e.ToString("yyyyMMdd_HHmmssfff");
        public static string ToLogTimestamp(this DateTime e) => e.ToString("yyyyMMdd-HHmmssfff");
        public static string ToShortTime(this DateTime e) => e.ToString("HH:mm:ss");
        public static string ToShortUsDate(this DateTime e) => e.ToString("MM/dd/yy");
        public static string ToCsvCell(this DateTime? e) => e.HasValue ? e.Value.ToString("yyyy-MM-dd HH:mm:ss.fff") : "[null]";
        public static int Week(this DateTime e)
        {
            DayOfWeek day = CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(e);
            if (day >= DayOfWeek.Monday && day <= DayOfWeek.Wednesday)
                e = e.AddDays(3);
            return CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(e, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        }
    }
}

