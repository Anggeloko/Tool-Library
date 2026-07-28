﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Axl.Base.Models;

namespace Axl.Base.Statics
{
    public static class Formats
    {
        public static string Path(string message)
        {
            if (string.IsNullOrEmpty(message)) return message;
            string[] words = message.Split(' ');
            for (int i = 0; i < words.Length; i++)
            {
                if (IsPath(words[i]))
                    words[i] = GetLastSegment(words[i]);
            }
            return string.Join(" ", words);
        }
        private static bool IsPath(string text) => (text.Contains("\\") || text.Contains("/"));
        private static string GetLastSegment(string path)
        {
            string[] segments = path.Split(new char[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
            return segments.Length > 0 ? segments[segments.Length - 1] : path;
        }
    }

    public static class EnvironmentParser
    {
        public static EnvironmentType Parse(string environmentText)
        {
            string normalizedText = "";
            if (environmentText != null)
                normalizedText = environmentText.Trim().ToLower();

            if (normalizedText.Contains("prod"))
                return EnvironmentType.Production;
            else if (normalizedText.Contains("dev"))
                return EnvironmentType.Development;
            else if (normalizedText.Contains("deb"))
                return EnvironmentType.Debug;
            else
                return EnvironmentType.Production;
        }
    }

}

