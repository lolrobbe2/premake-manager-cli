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
            await AnsiConsole.Progress().Columns(new ProgressColumn[]
            {
                            new TaskDescriptionColumn(),
                            new ProgressBarColumn(),
                            new PercentageColumn(),
                            new DownloadedColumn(),
                            new TransferSpeedColumn()
            }).StartAsync(async ctx =>
            {
                ProgressTask downloadTask = ctx.AddTask($"[green]{description}[/]");

                ProgressTaskSettings settings = new();
                HttpClient httpClient = new HttpClient();
                downloadTask.StartTask();
                using (HttpResponseMessage response = await httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();
                    downloadTask.MaxValue = response.Content.Headers.ContentLength!.Value;
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

                            downloadTask.Value += bytesRead;
                        }
                    }

                    response.EnsureSuccessStatusCode();
                }
            });
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
                ctx.Spinner(Spinner.Known.Aesthetic);
                ctx.SpinnerStyle(Style.Parse("green"));
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
            });
        }
    }
}
