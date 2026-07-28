﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Axl.Base.Models;

namespace Axl.Base.Sftp.Interfaces
{
    public interface ISftpService
    {
        /// <summary>
        /// Uploads a single file to the SFTP server.
        /// </summary>
        Task<Result<string>> UploadFile(string localFile, string remoteFolder, string remoteFileNameWithoutExtension = null);

        /// <summary>
        /// Uploads a folder and its contents recursively to the SFTP server.
        /// </summary>
        Task<Result<List<string>>> UploadFolder(string localFolder, string remoteFolder);

        /// <summary>
        /// Downloads a single file from the SFTP server.
        /// </summary>
        Task<Result<string>> DownloadFile(string remoteFile, string localFolder);

        /// <summary>
        /// Downloads a folder and its contents recursively from the SFTP server.
        /// </summary>
        Task<Result<List<string>>> DownloadFolder(string remoteFolder, string localFolder);

        /// <summary>
        /// Checks if the SFTP server is reachable.
        /// </summary>
        Task<Result<bool>> CheckAsync();
    }
}

