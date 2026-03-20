using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using YamlDotNet.Serialization;

namespace src.dependencies.types
{
    [DebuggerDisplay("{name} -> {version}")]
    [YamlSerializable]
    internal class LibraryDependency
    {
        [YamlMember]
        public string name { get; set; } = ""; // format: <owner>/<repo_name>

        [YamlMember]
        public string version { get; set; } = "";// format: "*", "=x.y.z", ">x.y.z", "<x.y.z", ">=x.y.z", "<=x.y.z", "@ => parent version"

        public bool IsValid() => LibraryDependencyValidator.ValidateLibrary(name, version);

        public override bool Equals(object? obj)
        {
            //same name -> same key
            if (obj is LibraryDependency other)
            {
                return string.Equals(name, other.name, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        public override int GetHashCode()
        {
            // Use the same property here as in Equals
            return name?.GetHashCode(StringComparison.OrdinalIgnoreCase) ?? 0;
        }
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
