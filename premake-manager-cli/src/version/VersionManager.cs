using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
        static IReadOnlyList<Release>? tags;
        static string repository = "premake-core";
        public static async Task<IReadOnlyList<Release>> GetVersions()
        {
            if (tags == null)
            {
                tags = await Github.Repositories.Release.GetAll(owner, repository);
            }
            return tags;
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
                ctx.Spinner(Spinner.Known.Dots);
                ctx.SpinnerStyle(Style.Parse("green"));
                return await Github.Repositories.Release.GetAllAssets(owner, repository, release.Id);
            });

            if (assets != null)
                AnsiConsole.WriteLine
            string platform = GetPlatformIdentifier();

            
            ReleaseAsset releaseAsset = assets.FirstOrDefault(asset => asset.Name.Contains(platform, StringComparison.OrdinalIgnoreCase))!;

            return false;
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
    }
}
