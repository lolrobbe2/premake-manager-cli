using Spectre.Console;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace src.utils
{
    internal class DownloadUtils
    {
        /// <summary>
        /// Downloads the given url with a download progress bar.
        /// </summary>
        /// <param name="downloadUrl">Url to download</param>
        /// <param name="description">description to show with the progress bar</param>
        /// <param name="destinationPath">folder path to save the file in.</param>
        public static async Task DownloadProgress(string downloadUrl,string description, string destinationPath)
        {
            await DownloadMultipleProgress(new[] { (downloadUrl, description, destinationPath) });
        }
        public static async Task DownloadMultipleProgress((string url, string description, string destinationPath)[] downloads)
        {
            await AnsiConsole.Progress().Columns(new ProgressColumn[]
            {
                            new TaskDescriptionColumn(),
                            new ProgressBarColumn(),
                            new PercentageColumn(),
                            new DownloadedColumn(),
                            new TransferSpeedColumn()
            }).StartAsync(async ctx =>
            {
                List<Task> tasks = new List<Task>();

                foreach (var download in downloads)
                    tasks.Add(DownloadProgressCtx(ctx, download.url, download.description, download.destinationPath));
                
                await Task.WhenAll(tasks);
            });
        }
        public static async Task DownloadProgressCtx(ProgressContext ctx, string downloadUrl, string description, string destinationPath)
        {
            ProgressTask downloadTask = ctx.AddTask($"[green]{description}[/]");
            downloadTask.StartTask();

            destinationPath = destinationPath.Replace("\\", "/");
            string destinationDir = Path.GetDirectoryName(destinationPath);
            if (!Directory.Exists(destinationDir))
                Directory.CreateDirectory(destinationDir);
            else
                PathUtils.ClearDirectory(destinationDir);

            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.ExpectContinue = false;
            using (HttpResponseMessage response = await httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();

                downloadTask.MaxValue = (double)response.Content.Headers.ContentLength!;

                using (Stream contentStream = await response.Content.ReadAsStreamAsync(),
                              fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    long totalBytesRead = 0;
                    byte[] buffer = new byte[8192];
                    int bytesRead;

                    while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead);
                        totalBytesRead += bytesRead;
                        downloadTask.Value += bytesRead;
                    }
                    contentStream.Close();
                }
            }
            downloadTask.StopTask();
        }

        /// <summary>
        ///  Downloads the given url with a Status visual.
        /// </summary>
        /// <param name="downloadUrl">Url to download</param>
        /// <param name="description">description to show with the Status visual</param>
        /// <param name="destinationPath">folder path to save the file in.</param>
        /// <returns></returns>
        public static async Task DownloadStatus(string downloadUrl, string description, string destinationPath)
        {
            await AnsiConsole.Status().StartAsync(description, async ctx =>
            {
                await DownloadStatusCtx(ctx, downloadUrl, destinationPath);
            });
        }

        public static async Task DownloadStatusCtx(StatusContext ctx, string downloadUrl, string destinationPath)
        {
            ctx.Spinner(Spinner.Known.Aesthetic);
            ctx.SpinnerStyle(Style.Parse("green"));

            destinationPath = destinationPath.Replace("\\", "/");
            string destinationDir = Path.GetDirectoryName(destinationPath);
            if (!Directory.Exists(destinationDir))
                Directory.CreateDirectory(destinationDir);
            else
                PathUtils.ClearDirectory(destinationDir);

            HttpClient httpClient = new HttpClient();
            using (HttpResponseMessage response = await httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();
                using (Stream contentStream = await response.Content.ReadAsStreamAsync(),
                      fileStream = new FileStream(destinationPath, System.IO.FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    long totalBytesRead = 0;
                    var buffer = new byte[8192]; // 8 KB buffer
                    int bytesRead;

                    while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead);
                        totalBytesRead += bytesRead;
                    }
                }
                
                response.EnsureSuccessStatusCode();
            }
        }
    }
}
