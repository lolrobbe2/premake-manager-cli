using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Octokit;
using Spectre.Console;
#nullable enable
namespace src.version
{
    internal class VersionManager
    {
        static string owner = "premake";
        static IReadOnlyList<Release>? releases;
        static string repository = "premake-core";
        public static async Task<IReadOnlyList<Release>> GetVersions()
        {
            if (releases == null)
            {
                releases = await AnsiConsole.Status().StartAsync("Fetching releases", async ctx => {
                    ctx.Spinner(Spinner.Known.Aesthetic);
                    ctx.SpinnerStyle(Style.Parse("green"));
                    return await Github.Repositories.Release.GetAll(owner, repository);
                });
            }
            return releases;
        }
        public static async Task<Release?> GetVersion(string tagName)
        {

            return (await GetVersions()).FirstOrDefault(release => release.TagName == tagName);
        }
        public static async Task<bool> InstallRelease(string tagName)
        {
           return await InstallRelease((await GetVersion(tagName))!);
        }
        public static async Task<bool> InstallRelease(Release release)
        {
            IReadOnlyList<ReleaseAsset> assets = await AnsiConsole.Status().StartAsync("Fetching releases", async ctx => {
                ctx.Spinner(Spinner.Known.Aesthetic);
                ctx.SpinnerStyle(Style.Parse("green"));
                return await Github.Repositories.Release.GetAllAssets(owner, repository, release.Id);
            });

            if (assets == null)
            {
                AnsiConsole.MarkupLine($"{Spectre.Console.Emoji.Known.CrossMark}  [red]Error:[/] unable to aquire release assets!");
                return false;
            }
            
            AnsiConsole.MarkupLine($"{Spectre.Console.Emoji.Known.CheckMark}  [green]Success: Fetching Release[/]");
            string platform = GetPlatformIdentifier();

            AnsiConsole.MarkupLine($"{Spectre.Console.Emoji.Known.DesktopComputer}  [dim white]platform: {platform}[/]");
            ReleaseAsset releaseAsset = assets!.FirstOrDefault(asset => asset.Name.Contains(platform, StringComparison.OrdinalIgnoreCase))!;
            #region DOWNLOAD_AND_EXTRACT_PREMAKE
            await AnsiConsole.Progress().Columns(new ProgressColumn[]
            {
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new DownloadedColumn(),
                new TransferSpeedColumn()
            }).StartAsync(async ctx =>
            {
                ProgressTask downloadTask = ctx.AddTask($"[green]downloading {release.Name}[/]");

                ProgressTaskSettings settings = new();
                ProgressTask extractTask = ctx.AddTaskAfter($"[green]extracting {release.Name}[/]", settings, downloadTask);
                HttpClient httpClient = new HttpClient();
                downloadTask.StartTask();
                string destinationPath = getPremakeReleasePath(release) + releaseAsset.Name;

                string destinationDirectory = getPremakeReleasePath(release);

                if (!Directory.Exists(destinationDirectory))
                {
                    Directory.CreateDirectory(destinationDirectory); // Create all missing directories
                }
                #region DOWNLOAD_PREMAKE
                using (HttpResponseMessage response = await httpClient.GetAsync(releaseAsset.BrowserDownloadUrl, HttpCompletionOption.ResponseHeadersRead))
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
                #endregion

                #region EXTRACT_PREMAKE
                using (ZipArchive archive = ZipFile.OpenRead(destinationPath))
                {
                    // Calculate the total size of all files in the archive
                    long totalUncompressedSize = 0;
                    foreach (var entry in archive.Entries)
                        totalUncompressedSize += entry.Length;

                    extractTask.MaxValue = totalUncompressedSize;

                    // Extract each entry while tracking the progress
                    foreach (var entry in archive.Entries)
                    {
                        if (string.IsNullOrEmpty(entry.Name)) // Skip directories
                            continue;

                        string destinationExtractPath = Path.Combine(getPremakeReleasePath(release), entry.FullName);

                        // Create subdirectories if needed
                        string destinationExtractDirectory = Path.GetDirectoryName(destinationExtractPath) ?? throw new ArgumentException("Invalid path.");
                        if (!Directory.Exists(destinationExtractDirectory))
                            Directory.CreateDirectory(destinationExtractDirectory);


                        // Extract the file
                        entry.ExtractToFile(destinationExtractPath, overwrite: true);

                        // Update progress based on the file size
                        extractTask.Value += entry.Length;
                    }
                }
                /* Delete the redundant zip folder */
                File.Delete(destinationPath);
                #endregion
            });


            #endregion
            return true;
        }

        public static async Task<bool> SetVersion(string tagName)
        {
            Release? release = await GetVersion(tagName);
            string path = Environment.GetEnvironmentVariable("Path")!;
            Console.WriteLine(path);
            return true;
        }
        private static string GetPlatformIdentifier()
        {
            // Identify the current platform
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Unix:
                    return "linux";
                case PlatformID.MacOSX:
                    return "macosx";
                case PlatformID.Win32NT:
                    return "windows";
                default:
                    return "unknown";
            }
        }

        private static string getPremakeRoamingPath()
        {
           return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/premakeManager/";
        }
        
        private static string getPremakeReleasePath(Release release)
        {
            return $"{getPremakeRoamingPath()}{release.TagName}/"; 
        }
    }
}
