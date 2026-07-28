﻿using System;
using System.Collections.Generic;

namespace Axl.Base.Statics
{
    /// <summary>
    /// Utility class for standardized exception formatting and trace extraction.
    /// </summary>
    public static class ExceptionUtils
    {
        /// <summary>
        /// Recursively formats an exception and its inner exceptions into a list of readable strings.
        /// </summary>
        /// <param name="ex">The exception to format.</param>
        /// <param name="tag">An optional tag or category to identify the context of the error.</param>
        /// <returns>A list of strings containing the exception messages and context.</returns>
        public static List<string> Format(Exception ex, string tag = "")
        {
            List<string> r = new List<string>();
            try
            {
                string context = string.IsNullOrEmpty(tag) ? "" : $"[{tag}] ";
                r.Add($"{context}Error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    r.AddRange(Format(ex.InnerException, "Inner"));
                }
            }
            catch
            {
                r.Add("Error formatting exception");
            }
            return r;
        }
    }
}

