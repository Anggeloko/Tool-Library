using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Axl.Base.Interfaces;
using Axl.Base.Models;
using Axl.Base.Sftp.Interfaces;
using Axl.Base.Sftp.Models;
using Axl.Base.Statics;

namespace Axl.Base.Sftp.Services
{
    /// <summary>
    /// Service for SFTP operations including recursive file and directory transfers.
    /// Built on SSH.NET for .NET Framework 4.5.2 compatibility.
    /// </summary>
    public class SftpService : ISftpService, ICheckable
    {
        private readonly SftpConfig _config;
        private readonly ILog _log;

        /// <summary>
        /// Initializes a new instance of the <see cref="SftpService"/> class.
        /// </summary>
        /// <param name="config">Connection settings for the SFTP server.</param>
        /// <param name="log">Optional logger implementation.</param>
        public SftpService(SftpConfig config, ILog log = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _log = log;
        }

        private SftpClient CreateClient()
        {
            return new SftpClient(_config.Host, _config.Port, _config.User, _config.Password);
        }

        public async Task<Result<bool>> CheckAsync()
        {
            try
            {
                using (var client = CreateClient())
                {
                    await Task.Run(() => client.Connect());
                    bool connected = client.IsConnected;
                    if (connected) client.Disconnect();
                    return Result<bool>.Success(connected);
                }
            }
            catch (Exception ex)
            {
                _log?.Error($"SFTP Check failed: {ex.Message}");
                return Result<bool>.Failure(ExceptionUtils.Format(ex, "SftpCheck"));
            }
        }

        /// <summary>
        /// Uploads a single file to a specific remote folder.
        /// </summary>
        /// <param name="localFile">Full path to the local file.</param>
        /// <param name="remoteFolder">Subfolder relative to the RemotePath defined in config.</param>
        /// <param name="remoteFileNameWithoutExtension">Optional: Rename the file on the server.</param>
        /// <returns>The full path of the uploaded file on the server.</returns>
        public async Task<Result<string>> UploadFile(string localFile, string remoteFolder, string remoteFileNameWithoutExtension = null)
        {
            try
            {
                if (!File.Exists(localFile))
                    return Result<string>.Failure($"Local file '{localFile}' does not exist.");

                string fileName = Path.GetFileName(localFile);
                if (!string.IsNullOrEmpty(remoteFileNameWithoutExtension))
                {
                    string ext = Path.GetExtension(localFile);
                    fileName = remoteFileNameWithoutExtension + ext;
                }

                string remoteBase = NormalizePath(_config.RemotePath);
                string subFolder = NormalizePath(remoteFolder);
                string remoteDirPath = CombineRemotePaths(remoteBase, subFolder);
                string remoteFilePath = CombineRemotePaths(remoteDirPath, fileName);

                using (var client = CreateClient())
                {
                    await Task.Run(() => client.Connect());
                    
                    CreateRemoteFolder(client, remoteDirPath);

                    using (var stream = File.OpenRead(localFile))
                    {
                        await Task.Run(() => client.UploadFile(stream, remoteFilePath));
                    }

                    _log?.Info($"Uploaded file to: {remoteFilePath}");
                    client.Disconnect();
                    return Result<string>.Success(remoteFilePath);
                }
            }
            catch (Exception ex)
            {
                _log?.Error($"SFTP Upload failed: {ex.Message}");
                return Result<string>.Failure(ExceptionUtils.Format(ex, "SftpUpload"));
            }
        }

        /// <summary>
        /// Uploads an entire local directory and its contents recursively to the server.
        /// </summary>
        /// <param name="localFolder">Full path to the local directory.</param>
        /// <param name="remoteFolder">Relative destination folder on the server.</param>
        /// <returns>A list of all successfully uploaded file paths.</returns>
        public async Task<Result<List<string>>> UploadFolder(string localFolder, string remoteFolder)
        {
            try
            {
                if (!Directory.Exists(localFolder))
                    return Result<List<string>>.Failure($"Local folder '{localFolder}' does not exist.");

                var uploadedFiles = new List<string>();
                string remoteBase = NormalizePath(_config.RemotePath);
                string subFolder = NormalizePath(remoteFolder);
                string remoteDirPath = CombineRemotePaths(remoteBase, subFolder);

                using (var client = CreateClient())
                {
                    await Task.Run(() => client.Connect());
                    
                    var errors = new List<string>();
                    await Task.Run(() => UploadRecursive(client, localFolder, remoteDirPath, uploadedFiles, errors));

                    client.Disconnect();

                    if (errors.Count > 0)
                        return Result<List<string>>.Failure(errors);

                    return Result<List<string>>.Success(uploadedFiles);
                }
            }
            catch (Exception ex)
            {
                _log?.Error($"SFTP Folder Upload failed: {ex.Message}");
                return Result<List<string>>.Failure(ExceptionUtils.Format(ex, "SftpFolderUpload"));
            }
        }

        public async Task<Result<string>> DownloadFile(string remoteFile, string localFolder)
        {
            try
            {
                string remoteBase = NormalizePath(_config.RemotePath);
                string subPath = NormalizePath(remoteFile);
                string remoteFilePath = CombineRemotePaths(remoteBase, subPath);
                
                string fileName = Path.GetFileName(remoteFilePath);
                string localFilePath = Path.Combine(localFolder, fileName);

                if (!Directory.Exists(localFolder))
                    Directory.CreateDirectory(localFolder);

                using (var client = CreateClient())
                {
                    await Task.Run(() => client.Connect());

                    if (!client.Exists(remoteFilePath))
                    {
                        client.Disconnect();
                        return Result<string>.Failure($"Remote file '{remoteFilePath}' does not exist.");
                    }

                    using (var stream = File.Create(localFilePath))
                    {
                        await Task.Run(() => client.DownloadFile(remoteFilePath, stream));
                    }

                    _log?.Info($"Downloaded file to: {localFilePath}");
                    client.Disconnect();
                    return Result<string>.Success(localFilePath);
                }
            }
            catch (Exception ex)
            {
                _log?.Error($"SFTP Download failed: {ex.Message}");
                return Result<string>.Failure(ExceptionUtils.Format(ex, "SftpDownload"));
            }
        }

        public async Task<Result<List<string>>> DownloadFolder(string remoteFolder, string localFolder)
        {
            try
            {
                string remoteBase = NormalizePath(_config.RemotePath);
                string subFolder = NormalizePath(remoteFolder);
                string remoteDirPath = CombineRemotePaths(remoteBase, subFolder);

                if (!Directory.Exists(localFolder))
                    Directory.CreateDirectory(localFolder);

                var downloadedFiles = new List<string>();

                using (var client = CreateClient())
                {
                    await Task.Run(() => client.Connect());
                    
                    var errors = new List<string>();
                    await Task.Run(() => DownloadRecursive(client, remoteDirPath, localFolder, downloadedFiles, errors));

                    client.Disconnect();

                    if (errors.Count > 0)
                        return Result<List<string>>.Failure(errors);

                    return Result<List<string>>.Success(downloadedFiles);
                }
            }
            catch (Exception ex)
            {
                _log?.Error($"SFTP Folder Download failed: {ex.Message}");
                return Result<List<string>>.Failure(ExceptionUtils.Format(ex, "SftpFolderDownload"));
            }
        }

        #region Private Helpers

        private void UploadRecursive(SftpClient client, string localPath, string remotePath, List<string> uploaded, List<string> errors)
        {
            try
            {
                CreateRemoteFolder(client, remotePath);

                foreach (string file in Directory.GetFiles(localPath))
                {
                    string fileName = Path.GetFileName(file);
                    string remoteFilePath = CombineRemotePaths(remotePath, fileName);

                    using (var stream = File.OpenRead(file))
                    {
                        client.UploadFile(stream, remoteFilePath);
                        uploaded.Add(remoteFilePath);
                    }
                }

                foreach (string dir in Directory.GetDirectories(localPath))
                {
                    string dirName = Path.GetFileName(dir);
                    string nextRemotePath = CombineRemotePaths(remotePath, dirName);
                    UploadRecursive(client, dir, nextRemotePath, uploaded, errors);
                }
            }
            catch (Exception ex)
            {
                errors.AddRange(ExceptionUtils.Format(ex, $"UploadRecursive: {localPath}"));
            }
        }

        private void DownloadRecursive(SftpClient client, string remotePath, string localPath, List<string> downloaded, List<string> errors)
        {
            try
            {
                var elements = client.ListDirectory(remotePath);
                foreach (var item in elements)
                {
                    if (item.Name == "." || item.Name == "..") continue;

                    string localItemPath = Path.Combine(localPath, item.Name);

                    if (item.IsDirectory)
                    {
                        if (!Directory.Exists(localItemPath))
                            Directory.CreateDirectory(localItemPath);
                        
                        DownloadRecursive(client, item.FullName, localItemPath, downloaded, errors);
                    }
                    else if (item.IsRegularFile)
                    {
                        using (var stream = File.Create(localItemPath))
                        {
                            client.DownloadFile(item.FullName, stream);
                            downloaded.Add(localItemPath);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                errors.AddRange(ExceptionUtils.Format(ex, $"DownloadRecursive: {remotePath}"));
            }
        }

        private void CreateRemoteFolder(SftpClient client, string remotePath)
        {
            string currentPath = "";
            var parts = remotePath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            
            // Handle leading slash
            if (remotePath.StartsWith("/")) currentPath = "/";

            foreach (var part in parts)
            {
                currentPath = CombineRemotePaths(currentPath, part);
                if (!client.Exists(currentPath))
                {
                    client.CreateDirectory(currentPath);
                    _log?.Debug($"Created remote directory: {currentPath}");
                }
            }
        }

        private string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path)) return "";
            return path.Replace("\\", "/").Trim('/');
        }

        private string CombineRemotePaths(string p1, string p2)
        {
            if (string.IsNullOrEmpty(p1)) return p2;
            if (string.IsNullOrEmpty(p2)) return p1;
            return $"{p1.TrimEnd('/')}/{p2.TrimStart('/')}";
        }

        #endregion
    }
}

