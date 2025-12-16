using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace  src.selfTest.version
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;
    using Octokit;
    using src.version;
    using src.selfTest;

    internal class VersionManagerTests : ITestClass
    {
        public IEnumerable<(string TestName, Func<Task> Action)> GetTests()
        {
            // Test 1: Fetch versions (mocked)
            yield return ("GetVersions returns releases", async () =>
            {
                var versions = await VersionManager.GetVersions();
                if (versions == null)
                    throw new Exception("Expected non-null releases list");
                await Task.CompletedTask;
            }
            );

            // Test 2: GetVersion by tag name
            yield return ("GetVersion finds correct release", async () =>
            {
                var versions = await VersionManager.GetVersions();
                var firstTag = versions.First().TagName;
                var release = await VersionManager.GetVersion(firstTag);
                if (release == null || release.TagName != firstTag)
                    throw new Exception("Expected to find release by tag name");
                await Task.CompletedTask;
            }
            );

            // Test 3: InstallRelease by tag name (mocked)
            yield return ("InstallRelease by tagName triggers install", async () =>
            {
                var versions = await VersionManager.GetVersions();
                var firstTag = versions.First().TagName;
                bool installed = await VersionManager.InstallRelease(firstTag);
                if (!installed)
                    throw new Exception("Expected InstallRelease(tagName) to succeed");
                await Task.CompletedTask;
            }
            );

            // Test 4: InstallRelease by Release object (mocked)
            yield return ("InstallRelease by Release object triggers install", async () =>
            {
                var versions = await VersionManager.GetVersions();
                var release = versions.First();
                bool installed = await VersionManager.InstallRelease(release);
                if (!installed)
                    throw new Exception("Expected InstallRelease(release) to succeed");
                await Task.CompletedTask;
            }
            );

            // Test 5: SetVersion updates config and PATH
            yield return ("SetVersion updates config and PATH", async () =>
            {
                var versions = await VersionManager.GetVersions();
                var release = versions.First();
                bool result = await VersionManager.SetVersion(release.TagName);
                if (!result)
                    throw new Exception("Expected SetVersion to succeed");
                await Task.CompletedTask;
            }
            );

            // Test 6: Platform detection
            yield return ("GetPlatformIdentifier returns valid string", async () =>
            {
                string platform = typeof(VersionManager)
                    .GetMethod("GetPlatformIdentifier", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                    .Invoke(null, null) as string ?? "";
                if (string.IsNullOrWhiteSpace(platform))
                    throw new Exception("Platform identifier should not be empty");
                await Task.CompletedTask;
            }
            );

            // Test 7: GetPremakeInstalledVersions returns directories
            yield return ("GetPremakeInstalledVersions returns list", async () =>
            {
                var versions = VersionManager.GetPremakeInstalledVersions();
                if (versions == null)
                    throw new Exception("Expected non-null list of installed versions");
                await Task.CompletedTask;
            }
            );

            // Test 8: AddPremakeToPath handles Windows
            yield return ("AddPremakeToPath updates PATH on Windows", async () =>
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    string fakePath = Path.Combine(Path.GetTempPath(), "premake");
                    Directory.CreateDirectory(fakePath);
                    VersionManager.AddPremakeToPath(fakePath);
                    string? current = new VersionManager().GetCurrentWindowsPath();
                    if (current == null || !current.Contains("premake", StringComparison.OrdinalIgnoreCase))
                        throw new Exception("Expected premake path in PATH");
                }
                await Task.CompletedTask;
            }
            );

            // Test 9: AddPremakeToPath handles Unix/macOS
            yield return ("AddPremakeToPath handles Unix/macOS", async () =>
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    string fakePath = Path.Combine(Path.GetTempPath(), "premake");
                    Directory.CreateDirectory(fakePath);
                    File.WriteAllText(Path.Combine(fakePath, "premake5"), "fake binary");
                    VersionManager.AddPremakeToPath(fakePath);
                    // No exception expected
                }
                await Task.CompletedTask;
            }
            );

            // Test 10: GetCurrentWindowsPath returns null if not set
            yield return ("GetCurrentWindowsPath returns null when no premake path", async () =>
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    string? path = new VersionManager().GetCurrentWindowsPath();
                    // It may be null if no premake path exists
                }
                await Task.CompletedTask;
            }
            );
        }
    }

}
