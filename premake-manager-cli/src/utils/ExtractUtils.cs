using Octokit;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace src.utils
{
    internal class ExtractUtils
    {
        public static async Task ExtractZipProgress()
        {
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

                    string destinationExtractPath = Path.Combine(GetPremakeReleasePath(release), entry.FullName);

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
        } 
    }
}
