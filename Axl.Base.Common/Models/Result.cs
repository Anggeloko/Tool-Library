﻿using System.Collections.Generic;

namespace Axl.Base.Models
{
    /// <summary>
    /// Standardized wrapper for service operation results, facilitating consistent error handling.
    /// </summary>
    /// <typeparam name="T">The type of the value returned on success.</typeparam>
    public class Result<T>
    {
        /// <summary>The data returned by the operation. Only valid if <see cref="IsSuccess"/> is true.</summary>
        public T Value { get; }
        
        /// <summary>A collection of error messages describing why the operation failed.</summary>
        public List<string> Errors { get; }
        
        /// <summary>The primary error message (first item in the Errors list).</summary>
        public string Error => (Errors != null && Errors.Count > 0) ? Errors[0] : string.Empty;
        
        /// <summary>Returns true if the operation completed without errors.</summary>
        public bool IsSuccess => Errors == null || Errors.Count == 0;

        private Result(T value, List<string> errors)
        {
            Value = value;
            Errors = errors ?? new List<string>();
        }

        /// <summary>Creates a successful result containing the specified value.</summary>
        public static Result<T> Success(T value) => new Result<T>(value, null);

        /// <summary>Creates a failed result with a list of error messages.</summary>
        public static Result<T> Failure(List<string> errors) => new Result<T>(default(T), errors);

        /// <summary>Creates a failed result with a single error message.</summary>
        public static Result<T> Failure(string error) => new Result<T>(default(T), new List<string> { error });
    }
}

