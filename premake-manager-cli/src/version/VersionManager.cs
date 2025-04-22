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

            string destinationPath = GetPremakeReleasePath(release) + releaseAsset.Name;

            await DownloadUtils.DownloadProgress(releaseAsset.BrowserDownloadUrl,$"Downloading premake {releaseAsset.Name}", destinationPath);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                await ExtractUtils.ExtractZipProgress(destinationPath, GetPremakeReleasePath(release), $"extracting premake");
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                await ExtractUtils.ExtractTarGzProgress(destinationPath, GetPremakeReleasePath(release), $"extracting premake");
                File.SetUnixFileMode(GetPremakeReleasePath(release) + "/premake5", UnixFileMode.UserRead | UnixFileMode.UserExecute | UnixFileMode.UserWrite);
            }

            await SetVersion(release.TagName);
            return true;
        }
        #endregion
        public static async Task<bool> SetVersion(string tagName)
        {
            Release? release = await GetVersion(tagName);
            string path = GetPremakeReleasePath(release!);
            AddPremakeToPath(path);
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

        /// <summary>
        /// returns the premake AppData folder
        /// </summary>
        /// <returns>
        /// string containing the premakeManger appData folder
        /// </returns>
        public static string GetPremakeRoamingPath()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/premakeManager/";
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "local",
                    "bin",
                    "premakeManager"
                );
            else
                return string.Empty;
        }
        private static string GetPremakeReleasePath(string tagName)
        {
            return $"{GetPremakeRoamingPath()}{tagName}/";
        }
        private static string GetPremakeReleasePath(Release release)
        {
            return GetPremakeReleasePath(release.TagName);
        }

        #endregion

        public static IList<string> GetPremakeInstalledVersions()
        {
            return Directory.GetDirectories(GetPremakeRoamingPath()).Where(x => Path.GetFileName(x)!.StartsWith('v')).ToList();
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
            string shell = Environment.GetEnvironmentVariable("SHELL") ?? "";
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string rcFile = shell.Contains("zsh") ? ".zshrc" : ".bashrc";
            string rcPath = Path.Combine(home, rcFile);

            string[] lines = File.Exists(rcPath) ? File.ReadAllLines(rcPath) : Array.Empty<string>();

            // Remove old Premake paths from export lines
            var updatedLines = lines
                .Where(line => !line.Contains("premake", StringComparison.OrdinalIgnoreCase))
                .ToList();

            string exportLine = $"export PATH=\"$PATH:{newPath}\"";

            // Append the new line if it's not already there
            updatedLines.Add("# Updated by PremakeManager");
            updatedLines.Add(exportLine);

            File.WriteAllLines(rcPath, updatedLines);

            AnsiConsole.MarkupLine($"[green]Updated {rcFile} with new Premake version.[/]");
            AnsiConsole.MarkupLine($"[grey]Run [blue]source ~/{rcFile}[/] or restart your terminal session.[/]");
        }

        #endregion
    }
} 
