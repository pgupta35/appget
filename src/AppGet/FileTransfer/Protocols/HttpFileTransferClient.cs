﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AppGet.FileSystem;
using AppGet.Http;
using AppGet.Manifests;
using AppGet.ProgressTracker;

namespace AppGet.FileTransfer.Protocols
{
    public class HttpFileTransferClient : IFileTransferClient
    {
        private readonly IHttpClient _httpClient;
        private readonly IFileSystem _fileSystem;
        private static readonly Regex HttpRegex = new Regex(@"^https?\:\/\/", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex FileNameRegex = new Regex(@"\.(zip|7zip|7z|rar|msi|exe)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);



        private static readonly Dictionary<string, WebHeaderCollection> HeaderCache = new Dictionary<string, WebHeaderCollection>();


        public HttpFileTransferClient(IHttpClient httpClient, IFileSystem fileSystem)
        {
            _httpClient = httpClient;
            _fileSystem = fileSystem;
        }

        public bool CanHandleProtocol(string source)
        {
            return HttpRegex.IsMatch(source);
        }


        public async Task<string> GetFileName(string source)
        {
            var uri = new Uri(source);

            var fileName = Path.GetFileName(uri.LocalPath);

            if (FileNameRegex.IsMatch(fileName))
            {
                return fileName;
            }

            var resp = await _httpClient.Head(uri);

            if (resp.RequestMessage.RequestUri != uri)
            {
                return await GetFileName(resp.RequestMessage.RequestUri.ToString());
            }

            if (resp.Content.Headers.ContentDisposition != null)
            {
                return resp.Content.Headers.ContentDisposition.FileName;
            }

            throw new InvalidDownloadUrlException(source);

        }

        public void TransferFile(string source, string destinationFile, FileVerificationInfo fileVerificationInfo = null)
        {
            Exception error = null;
            var tempFile = $"{destinationFile}.APPGET_DOWNLOAD";
            var progress = new ProgressState();

            using (var webClient = new WebClientWithTimeout(TimeSpan.FromSeconds(10)))
            {
                if (_fileSystem.FileExists(destinationFile))
                {
                    _fileSystem.DeleteFile(destinationFile);
                }

                webClient.DownloadProgressChanged += (sender, e) =>
                {
                    progress.Completed = e.BytesReceived;
                    var client = (WebClient)sender;

                    var contentType = client.ResponseHeaders["Content-Type"];
                    if (contentType != null && contentType.Contains("text"))
                    {
                        error = new InvalidDownloadUrlException(client.BaseAddress, $"[ContentType={contentType}]");
                        client.CancelAsync();
                    }

                    if (e.TotalBytesToReceive > 0)
                    {
                        progress.Total = e.TotalBytesToReceive;
                    }
                    else
                    {
                        progress.Total = null;
                    }

                    OnStatusUpdated?.Invoke(progress);
                };

                webClient.DownloadFileCompleted += (sender, e) =>
                {
                    if (error == null)
                    {
                        error = e.Error;
                    }
                };

                webClient.DownloadFileAsync(new Uri(source), tempFile);

                while (webClient.IsBusy)
                {
                    Thread.Sleep(500);
                }

                if (error != null)
                {
                    if (_fileSystem.FileExists(tempFile))
                    {
                        _fileSystem.DeleteFile(tempFile);
                    }

                    throw error;
                }

                HeaderCache[source] = webClient.ResponseHeaders;
            }

            _fileSystem.Move(tempFile, destinationFile);
            OnCompleted?.Invoke(progress);
        }

        public async Task<string> ReadString(string source)
        {
            var req = new HttpRequestMessage(HttpMethod.Get, source);
            req.Headers.CacheControl = new CacheControlHeaderValue { NoCache = true, NoStore = true };

            var resp = await _httpClient.Send(req);
            return await resp.Content.ReadAsStringAsync();
        }


        public static WebHeaderCollection GetTransferHeaders(string url)
        {
            return HeaderCache.ContainsKey(url) ? HeaderCache[url] : null;
        }

        public Action<ProgressState> OnStatusUpdated { get; set; }
        public Action<ProgressState> OnCompleted { get; set; }
    }
}
