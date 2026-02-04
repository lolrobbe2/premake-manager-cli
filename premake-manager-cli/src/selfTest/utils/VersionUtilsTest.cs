using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using src.utils;
using Version = src.utils.Version;
namespace src.selfTest.utils
{
    internal class VersionUtilsTest : ITestClass
    {
        public IEnumerable<(string TestName, Func<Task> Action)> GetTests()
        {
            yield return ("versionFromString (limited)", async () =>
            {
                Version version = new Version("3.2.1");
                if (version.major != 3)
                    throw new Exception("Version.major should be 3");
                else if (version.minor != 2)
                    throw new Exception("Version.minor should be 2");
                else if (version.patch != 1)
                    throw new Exception("Version.patch should be 1");
                else if (version.VersionInt != 50462976)
                    throw new Exception("Version.VersionInt should be 50462976"); //HEX: 03 02 01 00
            }
            );
            yield return ("versionFromString (extended)", async () =>
            {
                Version version = new Version("3.2.1.1");
                if (version.major != 3)
                    throw new Exception("Version.major should be 3");
                else if (version.minor != 2)
                    throw new Exception("Version.minor should be 2");
                else if (version.patch != 1)
                    throw new Exception("Version.patch should be 1");
                else if (version.revision != 1)
                    throw new Exception("Version.patch should be 1");
                else if (version.VersionInt != 50462977)
                    throw new Exception("Version.VersionInt should be 50462977"); //HEX: 03 02 01 01
            }
            );
            yield return ("version2(3.2.2) > version1(3.2.1)", async () =>
            {
                Version version1 = new Version("3.2.1");
                Version version2 = new Version("3.2.2");
                if (!(version2 > version1))
                    throw new Exception("Version2 should be larger then Version1"); //HEX: 03 02 01 01
            }
            );
            yield return ("version1(3.2.1) < version2(3.2.2)", async () =>
            {
                Version version1 = new Version("3.2.1");
                Version version2 = new Version("3.2.2");
                if (!(version1 < version2))
                    throw new Exception("Version2 should be larger then Version1"); //HEX: 03 02 01 01
            }
            );
            yield return ("version2(3.2.1.1) > version1(3.2.1.0)", async () =>
            {
                Version version1 = new Version("3.2.1.0");
                Version version2 = new Version("3.2.1.1");
                if (!(version2 > version1))
                    throw new Exception("Version2 should be larger then Version1"); //HEX: 03 02 01 01
            }
            );
            yield return ("version1(3.2.1.0) < version2(3.2.1.1)", async () =>
            {
                Version version1 = new Version("3.2.1.0");
                Version version2 = new Version("3.2.1.1");
                if (!(version1 < version2))
                    throw new Exception("Version2 should be larger then Version1"); //HEX: 03 02 01 01
            }
            );
            yield return ("versionFromString (limited, suffix)", async () =>
            {
                Version version = new Version("3.2.1-alpha");
                if (version.major != 3)
                    throw new Exception("Version.major should be 3");
                else if (version.minor != 2)
                    throw new Exception("Version.minor should be 2");
                else if (version.patch != 1)
                    throw new Exception("Version.patch should be 1");
                else if (version.VersionInt != 50462976)
                    throw new Exception("Version.VersionInt should be 50462976"); //HEX: 03 02 01 00
            }
            );
            yield return ("versionFromString (extended, suffix)", async () =>
            {
                Version version = new Version("3.2.1.1-rc1");
                if (version.major != 3)
                    throw new Exception("Version.major should be 3");
                else if (version.minor != 2)
                    throw new Exception("Version.minor should be 2");
                else if (version.patch != 1)
                    throw new Exception("Version.patch should be 1");
                else if (version.revision != 1)
                    throw new Exception("Version.patch should be 1");
                else if (version.VersionInt != 50462977)
                    throw new Exception("Version.VersionInt should be 50462977"); //HEX: 03 02 01 01
            }
            );

            yield return ("rangeFromString", async () =>
            {
                VersionRange range = VersionUtils.GetRangeFromString(">=3.2.1");
            }
            );
        }
    }
}
