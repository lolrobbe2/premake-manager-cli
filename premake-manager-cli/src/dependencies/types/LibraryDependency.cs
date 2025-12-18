using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using YamlDotNet.Serialization;

namespace src.dependencies.types
{
    [YamlSerializable]
    internal class LibraryDependency
    {
        [YamlMember]
        public string name { get; set; } // format: <owner>/<repo_name>

        [YamlMember]
        public string version { get; set; } // format: "*", "=x.y.z", ">x.y.z", "<x.y.z", ">=x.y.z", "<=x.y.z"

        public bool IsValid() => LibraryDependencyValidator.ValidateLibrary(name, version);

        [YamlIgnore]
        public VersionRange VersionRange => new VersionRange(version);
    }

    internal static class LibraryDependencyValidator
    {
        private static readonly Regex VersionRegex =
            new Regex(@"^(\*|([<>]=?|=)\d+\.\d+\.\d+)$",
                      RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex NameRegex =
            new Regex(@"^[^/\s]+/[^/\s]+$", // owner/repo_name, no spaces or extra slashes
                      RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static bool ValidateVersion(string version)
        {
            if (string.IsNullOrWhiteSpace(version))
                return false;

            return VersionRegex.IsMatch(version);
        }

        public static bool ValidateName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            return NameRegex.IsMatch(name);
        }

        public static bool ValidateLibrary(string name, string version)
        {
            return ValidateName(name) && ValidateVersion(version);
        }
    }
}
