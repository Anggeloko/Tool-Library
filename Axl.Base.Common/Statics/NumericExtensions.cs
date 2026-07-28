﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Axl.Base.Statics
{
    public static class NumericExtensions
    {
        public static string Str(this double e) => e.ToString(System.Globalization.CultureInfo.InvariantCulture);
        public static string Str(this double? e) => e?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "";
        public static string Str(this float e) => e.ToString(System.Globalization.CultureInfo.InvariantCulture);
        public static string Str(this int e) => e.ToString();
    }
    public static class SecureRandom
    {
        // El código de SecureRandomInt va aquí
        public static int Int(int minValue = 0, int maxValue = 100)
        {
            if (minValue >= maxValue)
                throw new ArgumentException("minValue must be lower than maxValue");
            long range = (long)maxValue - minValue;

            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                byte[] uintBuffer = new byte[4];
                while (true)
                {
                    rng.GetBytes(uintBuffer);
                    uint rand = BitConverter.ToUInt32(uintBuffer, 0);

                    long maxVal = 1 + (long)uint.MaxValue;
                    long remainder = maxVal % range;

                    if (rand >= maxVal - remainder)
                        continue;

                    return (int)(minValue + (rand % range));
                }
            }
        }
        // El código de SecureRandomDouble va aquí
        public static double Double(double minValue = 0.0, double maxValue = 1.0)
        {
            if (minValue >= maxValue)
                throw new ArgumentException("minValue must be lower than maxValue");

            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                byte[] bytes = new byte[8]; // Usaremos 8 bytes para un UInt64
                rng.GetBytes(bytes);
                ulong ulongRand = BitConverter.ToUInt64(bytes, 0);
                double zeroToOneExclusive = ulongRand / (UInt64.MaxValue + 1.0D);
                double result = minValue + zeroToOneExclusive * (maxValue - minValue);
                return result;
            }
        }
    }

}
