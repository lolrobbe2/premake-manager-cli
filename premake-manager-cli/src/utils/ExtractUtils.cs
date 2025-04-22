using Octokit;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Spectre.Console;
using System.Formats.Tar;
#nullable enable
namespace src.utils
{
    internal class ExtractUtils
    {
        public static async Task ExtractZipProgress(string sourcePath, string destinationExtractDirectory, string description, bool deleteSource = true)
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
                await ExtractZipProgressCtx(ctx, sourcePath, destinationExtractDirectory, description, deleteSource);
            });

        }


        public static async Task ExtractTarGzProgress(string sourcePath, string destinationExtractDirectory, string description, bool deleteSource = true)
        {
            await AnsiConsole.Progress()
                .Columns(new ProgressColumn[]
                {
                    new TaskDescriptionColumn(),
                    new ProgressBarColumn(),
                    new PercentageColumn(),
                    new DownloadedColumn(),
                    new TransferSpeedColumn()
                })
                .StartAsync(async ctx =>
                {
                    ProgressTask extractTask = ctx.AddTask($"[green]{description}[/]");

                    if (!Directory.Exists(destinationExtractDirectory))
                        Directory.CreateDirectory(destinationExtractDirectory);

                    long totalSize = 0;
                    var entrySizes = new List<(string Name, long Size)>();

                    // Step 1: Pre-scan to estimate total size (optional, but nice for progress)
                    using (FileStream fs = File.OpenRead(sourcePath))
                    using (GZipStream gzip = new GZipStream(fs, CompressionMode.Decompress))
                    using (MemoryStream tarBuffer = new MemoryStream())
                    {
                        await gzip.CopyToAsync(tarBuffer);
                        tarBuffer.Seek(0, SeekOrigin.Begin);

                        using TarReader reader = new TarReader(tarBuffer, leaveOpen: true);
                        TarEntry? entry;
                        while ((entry = reader.GetNextEntry()) != null)
                        {
                            if (entry.EntryType == TarEntryType.RegularFile)
                            {
                                long entrySize = entry.Length;
                                entrySizes.Add((entry.Name, entrySize));
                                totalSize += entrySize;
                            }
                        }
                    }

                    extractTask.MaxValue = totalSize;

                    // Step 2: Extract with progress
                    using (FileStream fs = File.OpenRead(sourcePath))
                    using (GZipStream gzip = new GZipStream(fs, CompressionMode.Decompress))
                    using (TarReader reader = new TarReader(gzip))
                    {
                        TarEntry? entry;
                        while ((entry = reader.GetNextEntry()) != null)
                        {
                            string fullPath = Path.Combine(destinationExtractDirectory, entry.Name);

                            if (entry.EntryType == TarEntryType.Directory)
                            {
                                Directory.CreateDirectory(fullPath);
                            }
                            else if (entry.EntryType == TarEntryType.RegularFile)
                            {
                                string? dir = Path.GetDirectoryName(fullPath);
                                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                                    Directory.CreateDirectory(dir);

                                using FileStream outStream = File.Create(fullPath);
                                entry.DataStream!.CopyTo(outStream);

                                extractTask.Increment(entry.Length);
                            }
                        }
                    }
                    if (deleteSource)
                        File.Delete(sourcePath);
                });
        }

        private static async Task ExtractZipProgressCtx(ProgressContext ctx, string sourcePath, string destinationExtractDirectory, string description, bool deleteSource = true)
        {
            ProgressTask extractTask = ctx.AddTask($"[green]{description}[/]");

            using (ZipArchive archive = ZipFile.OpenRead(sourcePath))
            {
                long totalUncompressedSize = archive.Entries.Sum(e => e.Length);
                extractTask.MaxValue = totalUncompressedSize;

                if (!Directory.Exists(destinationExtractDirectory))
                    Directory.CreateDirectory(destinationExtractDirectory);

                object progressLock = new object();

                await Task.Run(() =>
                {
                    Parallel.ForEach(archive.Entries, entry =>
                    {
                        if (string.IsNullOrEmpty(entry.Name))
                            return;

                        string destinationPath = Path.Combine(destinationExtractDirectory, entry.FullName);
                        string? destinationDir = Path.GetDirectoryName(destinationPath);

                        if (!string.IsNullOrEmpty(destinationDir) && !Directory.Exists(destinationDir))
                            Directory.CreateDirectory(destinationDir);

                        entry.ExtractToFile(destinationPath, overwrite: true);

                        lock (progressLock)
                        {
                            extractTask.Value += entry.Length;
                        }
                    });
                });
                /* Delete the redundant zip folder */
                if (deleteSource)
                    File.Delete(sourcePath);
            }
        }

    }
}
