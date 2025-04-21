using Octokit;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Spectre.Console;
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

        public static async Task ExtractMultipleZipProgress((string sourcePath, string destinationExtractDirectory, string description, bool deleteSource)[]extractionTasks)
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

                foreach (var task in extractionTasks)
                    tasks.Add(ExtractZipProgressCtx(ctx, task.sourcePath, task.destinationExtractDirectory, task.description, task.deleteSource));
                await Task.WhenAll(tasks);
            });
        }

        public static async Task ExtractZipProgressCtx(ProgressContext ctx, string sourcePath, string destinationExtractDirectory, string description, bool deleteSource = true)
        {
            ProgressTask extractTask = ctx.AddTask($"[green]{description}[/]");

            ProgressTaskSettings settings = new();
            using (ZipArchive archive = ZipFile.OpenRead(sourcePath))
            {
                // Calculate the total size of all files in the archive
                long totalUncompressedSize = 0;
                foreach (var entry in archive.Entries)
                    totalUncompressedSize += entry.Length;

                extractTask.MaxValue = totalUncompressedSize;

                if (!Directory.Exists(destinationExtractDirectory))
                    Directory.CreateDirectory(destinationExtractDirectory);
                // Extract each entry while tracking the progress

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
            }

            /* Delete the redundant zip folder */
            if (deleteSource)
                File.Delete(sourcePath);
        }
    }
}
