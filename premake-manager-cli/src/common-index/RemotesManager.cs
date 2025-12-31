using src.utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace src.common_index
{
    /// <summary>
    /// premake manager Remote yaml format
    /// </summary>
    [YamlSerializable]
    internal sealed class Remote
    {
        [YamlMember(Alias ="owner", Order = 0)]
        public required string Owner {  get; set; }
        [YamlMember(Alias = "repo", Order = 1)]
        public required string Repo { get; set; }
        [YamlMember(Alias = "enabled", Order = 2)]
        public required bool Enabled { get; set; }
    }
    /// <summary>
    /// Class representation for the remotes
    /// </summary>
    [YamlSerializable]
    internal sealed class Remotes
    {
        [YamlMember(Alias = "remotes", Order = 0)]
        public required Remote[] remotes {get; set;}
    }

    internal sealed class RemoteIndex
    {
        public required Remote Remote { get; set; }
        public required IndexView Index {  get; set; }
    }
    internal class RemotesManager
    {
        private static readonly Remote DefaultRemote = new Remote() { Owner = "lolrobbe2", Repo = "premake-common-registry", Enabled = true };
        public static Remote[] Remotes { get => GetRemotes(); set => SetRemotes(value); }
        public static RemoteIndex[] RemoteIndices { get => GetRemoteIndices(); }
        public static RemoteIndex[] EnabledRemoteIndices { get => GetEnabledRemoteIndices(); }

        private static Remote[] GetRemotes()
        {
            string RemotePaths = Path.Combine(PathUtils.GetRoamingPath(), "premakeRemotes.yml");

            if (Path.Exists(RemotePaths))
            {
                return YamlSerializer.Deserialize<Remotes>(RemotePaths).remotes;
            }
            return [DefaultRemote];
        }

        private static RemoteIndex[] GetRemoteIndices()
        {
            UpdateRemotes().GetAwaiter().GetResult();
            return Remotes.Select((remote) => new RemoteIndex() { Remote = remote, Index = CommonIndex.ReadRemoteLocalIndex(remote.Owner, remote.Repo) }).ToArray();
        }

        private static RemoteIndex[] GetEnabledRemoteIndices()
        {
            return GetRemoteIndices()
                .Where((RemoteIndex remoteIndex) => remoteIndex.Remote.Enabled)
                .ToArray();
        }
        private static void SetRemotes(Remote[] remotes)
        {
            string RemotePaths = Path.Combine(PathUtils.GetRoamingPath(), "premakeRemotes.yml");

            YamlSerializer.Serialize(new Remotes() { remotes = remotes.ToArray() }, RemotePaths);
        }
        public static async Task AddRemote(string owner, string repo)
        {
            Remote remote = new Remote() { Owner = owner, Repo = repo, Enabled = true };
            IList<Remote> remotes = Remotes.ToList();
            remotes.Add(remote);
            Remotes = remotes.ToArray();
            await UpdateRemotes(true);
        }

        public static async Task RemoveRemote(string owner, string repo)
        {
            Remote remote = new Remote() { Owner = owner, Repo = repo, Enabled = true };
            IList<Remote> remotes = Remotes.ToList();
            remotes.Remove(remote);
            Remotes = remotes.ToArray();
            await UpdateRemotes(true);
        }

        /// <summary>
        /// This function checks how old the remotes are and updates them when needed
        /// </summary>
        private static async Task UpdateRemotes(bool force = false)
        {
            if (!force && !RemotesOutdated())
                return;
            await InstallRemotes();
        }

        private static async Task InstallRemotes()
        {
            PathUtils.ClearDirectory(PathUtils.GetRemotesPath()); //clear dir

            //install the remotes
            (string url, string description, string destinationPath)[] downloads =
      Remotes
          .Select((Remote remote) =>
          {
              string url = $"https://github.com/{remote.Owner}/{remote.Repo}/archive/refs/heads/main.{(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "zip" : "tar.gz")}";
              string description = $"Downloading Remote: {remote.Owner}/{remote.Repo}";
              return (
                  url,
                  description,
                  PathUtils.GetRemotePath(remote.Owner, remote.Repo)
              );
          })
          .ToArray();

            await DownloadUtils.DownloadMultipleProgress(downloads);
            CreateTimestamp();

        }

        private static void CreateTimestamp()
        {
            string timestamp = DateTime.UtcNow.ToString("O"); // ISO 8601, UTC
            File.WriteAllText(Path.Combine(PathUtils.GetRemotesPath(), ".timestamp"), timestamp, Encoding.UTF8);
        }

        private static bool RemotesOutdated()
        {
            string remotesPath = PathUtils.GetRemotesPath();
            string timestampFilePath = Path.Combine(remotesPath, ".timestamp");

            if (!File.Exists(timestampFilePath))
                return true; 

            string content = File.ReadAllText(timestampFilePath).Trim();

            if (!DateTime.TryParse(
                    content,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out DateTime timestamp))
                return true; // invalid timestamp -> outdated
            

            TimeSpan age = DateTime.UtcNow - timestamp;
            return age >= TimeSpan.FromDays(1);
        }
    }
}
