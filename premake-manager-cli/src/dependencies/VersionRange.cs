using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace src.dependencies
{
    internal class VersionRange
    {
        public long? LowerBound { get; private set; }
        public bool IncludeLower { get; private set; }
        public long? UpperBound { get; private set; }
        public bool IncludeUpper { get; private set; }
        public bool AnyVersion { get; private set; }

        public VersionRange(string version)
        {
            if (string.IsNullOrWhiteSpace(version))
                throw new ArgumentException("Version cannot be empty", nameof(version));

            if (version == "*")
            {
                AnyVersion = true;
                return;
            }

            IncludeLower = false;
            IncludeUpper = false;
            LowerBound = null;
            UpperBound = null;

            if (version.StartsWith(">="))
            {
                LowerBound = VersionToInt(version.Substring(2));
                IncludeLower = true;
            }
            else if (version.StartsWith(">"))
            {
                LowerBound = VersionToInt(version.Substring(1));
            }
            else if (version.StartsWith("<="))
            {
                UpperBound = VersionToInt(version.Substring(2));
                IncludeUpper = true;
            }
            else if (version.StartsWith("<"))
            {
                UpperBound = VersionToInt(version.Substring(1));
            }
            else if (version.StartsWith("="))
            {
                LowerBound = VersionToInt(version.Substring(1));
                UpperBound = LowerBound;
                IncludeLower = true;
                IncludeUpper = true;
            }
            else
            {
                LowerBound = VersionToInt(version);
                UpperBound = LowerBound;
                IncludeLower = true;
                IncludeUpper = true;
            }
        }

        private static long VersionToInt(string version)
        {
            var parts = version.Split('.');
            long major = parts.Length > 0 ? long.Parse(parts[0]) : 0;
            long minor = parts.Length > 1 ? long.Parse(parts[1]) : 0;
            long patch = parts.Length > 2 ? long.Parse(parts[2]) : 0;
            return major * 1_000_000 + minor * 1_000 + patch;
        }

        public bool Overlaps(VersionRange other)
        {
            if (AnyVersion || other.AnyVersion)
                return true;

            // this upper vs other lower
            if (UpperBound.HasValue && other.LowerBound.HasValue)
            {
                var cmp = UpperBound.Value.CompareTo(other.LowerBound.Value);
                if (cmp < 0 || (cmp == 0 && (!IncludeUpper || !other.IncludeLower)))
                    return false;
            }

            // other upper vs this lower
            if (other.UpperBound.HasValue && LowerBound.HasValue)
            {
                var cmp = other.UpperBound.Value.CompareTo(LowerBound.Value);
                if (cmp < 0 || (cmp == 0 && (!other.IncludeUpper || !IncludeLower)))
                    return false;
            }

            return true;
        }

        public static bool ConflictsWithAll(IEnumerable<VersionRange> ranges)
        {
            var list = new List<VersionRange>(ranges);
            for (int i = 0; i < list.Count; i++)
            {
                for (int j = i + 1; j < list.Count; j++)
                {
                    if (!list[i].Overlaps(list[j]))
                        return true; // conflict exists
                }
            }
            return false; // no conflict
        }
    }
}
