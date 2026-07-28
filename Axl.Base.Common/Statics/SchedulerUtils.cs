﻿using System;

namespace Axl.Base.Statics
{
    public static class SchedulerUtils
    {
        public static double GetNextHourlyInterval(double hoursCycle)
        {
            DateTime next = DateTime.Now.AddHours(hoursCycle);
            int h = next.Hour;
            next = next.Subtract(next.TimeOfDay).AddHours(h);

            double interval = (next - DateTime.Now).TotalMilliseconds;
            if (interval <= 0)
            {
                interval += TimeSpan.FromHours(hoursCycle).TotalMilliseconds;
            }
            return interval;
        }

        public static double GetNextMinuteInterval(double minutesCycle)
        {
            DateTime next = DateTime.Now.AddMinutes(minutesCycle);
            int h = next.Hour, m = next.Minute;
            next = next.Subtract(next.TimeOfDay).AddHours(h).AddMinutes(m);

            double interval = (next - DateTime.Now).TotalMilliseconds;
            if (interval <= 0)
            {
                interval += TimeSpan.FromMinutes(minutesCycle).TotalMilliseconds;
            }
            return interval;
        }

        public static double GetNextDailyAt(double hour)
        {
            DateTime next = DateTime.Now.AddDays(1);
            next = next.Subtract(next.TimeOfDay).AddHours(hour);
            return (next - DateTime.Now).TotalMilliseconds;
        }

        public static double GetDailyRunAt(double hour)
        {
            DateTime now = DateTime.Now;
            DateTime nextRun = now.Date.AddHours(hour);

            if (nextRun <= now)
            {
                nextRun = nextRun.AddDays(1);
            }

            return (nextRun - now).TotalMilliseconds;
        }

        public static double GetFixedSecondInterval(double seconds)
        {
            return seconds * 1000;
        }
    }
}

