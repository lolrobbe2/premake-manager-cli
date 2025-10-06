using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Octokit;
using Spectre.Console;
using src.config;
using src.utils;
#nullable enable
namespace src.version
{
    internal class VersionManager
    {
        static string owner = "premake";
        static IReadOnlyList<Release>? releases;
        static string repository = "premake-core";
        #region INSTALL_VERSIONS
        public static async Task<IReadOnlyList<Release>> GetVersions()
        {
            if (releases == null)
            {
                releases = await AnsiConsole.Status().StartAsync("Fetching releases", async ctx =>
                {
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
            IReadOnlyList<ReleaseAsset> assets = await AnsiConsole.Status().StartAsync("Fetching releases", async ctx =>
            {
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
            //DOWNLOAD_AND_EXTRACT_PREMAKE

            string destinationPath = PathUtils.GetReleasePath(release) + releaseAsset.Name;
            await AnsiConsole.Progress().Columns(new ProgressColumn[]
           {
                            new TaskDescriptionColumn(),
                            new ProgressBarColumn(),
                            new PercentageColumn(),
                            new DownloadedColumn(),
                            new TransferSpeedColumn()
           }).StartAsync(async ctx =>
           {
               await DownloadUtils.DownloadProgressCtx(ctx,releaseAsset.BrowserDownloadUrl, $"Downloading premake {releaseAsset.Name}", destinationPath);
               
               if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                   await ExtractUtils.ExtractZipProgressCtx(ctx,destinationPath, PathUtils.GetReleasePath(release), $"extracting premake");
               else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
               {
                   await ExtractUtils.ExtractTarGzProgressCtx(ctx, destinationPath, PathUtils.GetReleasePath(release), $"extracting premake");
                   File.SetUnixFileMode(PathUtils.GetReleasePath(release) + "/premake5", UnixFileMode.UserRead | UnixFileMode.UserExecute | UnixFileMode.UserWrite);
               }
           });
            await SetVersion(release.TagName);
            return true;
        }
        #endregion
        public static async Task<bool> SetVersion(string tagName)
        {
            Release? release = await GetVersion(tagName);
            string path = PathUtils.GetReleasePath(release!);
            AddPremakeToPath(path);
            ConfigReader reader = new ConfigReader();
            ConfigWriter configWriter = ConfigWriter.FromReader(reader);
            configWriter.SetVersion(tagName);
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
        #region LOCAL_VERSION_PATHS

        #endregion

        public static IList<string> GetPremakeInstalledVersions()
        {
            return Directory.GetDirectories(PathUtils.GetRoamingPath()).Where(x => Path.GetFileName(x)!.StartsWith('v')).ToList();
        }


        #region PATH_UTILITIES
        public static void AddPremakeToPath(string premakePath)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                UpdateWindowsPath(premakePath);
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                UpdateUnixPath(premakePath);
            else
                AnsiConsole.MarkupLine("[red]Unsupported OS for PATH modification.[/]");
        }

        // ------------------- Windows --------------------
        private static void UpdateWindowsPath(string newPath)
        {
            string currentPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User) ?? "";
            string[] parts = currentPath.Split(';');

            // Remove any existing Premake paths
            string[] filtered = parts
                .Where(p => !p.Contains("premake", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (filtered.Contains(newPath, StringComparer.OrdinalIgnoreCase))
            {
                AnsiConsole.MarkupLine("[yellow]Premake path already up-to-date in PATH.[/]");
                return;
            }

            string updatedPath = string.Join(";", filtered.Append(newPath));
            Environment.SetEnvironmentVariable("PATH", updatedPath, EnvironmentVariableTarget.User);

            AnsiConsole.MarkupLine("[green]Updated PATH with new Premake version.[/]");
            AnsiConsole.MarkupLine("[grey](You may need to restart your terminal.)[/]");
        }

        // ------------------- Linux/macOS --------------------
        private static void UpdateUnixPath(string newPath)
        {
            string symlinkPath = "/usr/local/bin/premake5";

            if (!File.Exists(Path.Combine(newPath,"premake5")))
            {
                AnsiConsole.MarkupLine($"[red]Executable not found: {Path.Combine(newPath,"premake5")}[/]");
                return;
            }

            try
            {
                if (File.Exists(symlinkPath))
                    File.Delete(symlinkPath);

                File.CreateSymbolicLink(symlinkPath, newPath);
                AnsiConsole.MarkupLine($"[green]Symlink created: {symlinkPath} → {Path.Combine(newPath, "premake5")}[/]");
            }
            catch (UnauthorizedAccessException)
            {
                AnsiConsole.MarkupLine($"[red]Permission denied.[/] Try running with [blue]sudo[/].");
            }
            catch (PlatformNotSupportedException)
            {
                AnsiConsole.MarkupLine($"[red]Symlinks are not supported on this platform or .NET version.[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Unexpected error:[/] {ex.Message}");
            }
        }

        public static string? GetCurrentVersionPath()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return GetCurrentWindowsPath();
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return GetCurrentUnixPath();
            else
                AnsiConsole.MarkupLine("[red]Unsupported OS for PATH retrieval.[/]");

            return null;//platform is not supported
        }

        // ------------------- Windows --------------------
        /// <summary>
        /// This returns the install path of the current premake version
        /// </summary>
        /// <returns></returns>
        public static string? GetCurrentWindowsPath()
        {
            string currentPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User) ?? "";
            string[] parts = currentPath.Split(';');

            // Remove any existing Premake paths
            string[] filtered = parts
                .Where(p => p.Contains("premake", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if( filtered.Length == 0 )
                return null;

            return filtered.First();
        }

        // ------------------- Linux/macOS --------------------

        /// <summary>
        /// This function returns the install dir on unix platform
        /// </summary>
        /// <returns></returns>
        private static string? GetCurrentUnixPath()
        {
            string symlinkPath = "/usr/local/bin/premake5";

            try
            {
                if (!File.Exists(symlinkPath))
                {
                    AnsiConsole.MarkupLine($"[yellow]Symlink not found: {symlinkPath}[/]");
                    return null;
                }

                // Get the target path of the symlink
                string? targetPath = File.ResolveLinkTarget(symlinkPath, false)?.FullName;

                if (string.IsNullOrEmpty(targetPath))
                {
                    AnsiConsole.MarkupLine($"[red]Failed to resolve symlink target for:[/] {symlinkPath}");
                    return null;
                }

                AnsiConsole.MarkupLine($"[green]Symlink target:[/] {targetPath}");
                return targetPath;
            }
            catch (UnauthorizedAccessException)
            {
                AnsiConsole.MarkupLine($"[red]Permission denied while reading symlink.[/]");
            }
            catch (PlatformNotSupportedException)
            {
                AnsiConsole.MarkupLine($"[red]Symlinks are not supported on this platform or .NET version.[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Unexpected error:[/] {ex.Message}");
            }

            return null;
        }

        #endregion
    }


} 
