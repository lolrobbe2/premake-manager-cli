﻿using Octokit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace src.utils
{
    internal class PathUtils
    {
        /// <summary>
        /// returns the premake AppData folder
        /// </summary>
        /// <returns>
        /// string containing the premakeManger appData folder
        /// </returns>
        public static string GetRoamingPath()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/premakeManager/";
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "lib",
                    "premakeManager"
                );
            else
                return string.Empty;
        }
        public static string GetPremakeReleasePath(string tagName)
        {
            return $"{GetRoamingPath()}{tagName}/";
        }
        public static string GetReleasePath(Release release)
        {
            return GetPremakeReleasePath(release.TagName);
        }

        public static string GetTempPath()
        {
            return Path.Combine(GetRoamingPath(), "temp");
        }

        public static string GetTempModuleInfoPath(string moduleName)
        {
            return Path.Combine(GetTempPath(), moduleName);
        }
    }
}
