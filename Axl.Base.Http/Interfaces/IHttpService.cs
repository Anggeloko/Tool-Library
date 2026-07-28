﻿using System.Threading.Tasks;
using Axl.Base.Models;

namespace Axl.Base.Http.Interfaces
{
    public interface IHttpService
    {
        /// <summary>
        /// Authenticates and retrieves a Bearer token.
        /// </summary>
        Task<Result<string>> GetTokenAsync();

        /// <summary>
        /// Uploads a file using Multipart/Form-Data.
        /// </summary>
        Task<Result<bool>> UploadFileAsync(string filePath, string endpoint);

        // --- Generic CRUD Methods ---

        Task<Result<TResponse>> GetAsync<TResponse>(string endpoint);
        Task<Result<TResponse>> PostAsync<TRequest, TResponse>(string endpoint, TRequest body);
        Task<Result<TResponse>> PutAsync<TRequest, TResponse>(string endpoint, TRequest body);
        Task<Result<TResponse>> PatchAsync<TRequest, TResponse>(string endpoint, TRequest body);
        Task<Result<bool>> DeleteAsync(string endpoint);
    }
}

