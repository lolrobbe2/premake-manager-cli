using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;

namespace src.utils
{
    class EnumHelper
    {
        public static string GetEnumValue<T>(T enumValue) where T : Enum
        {
            var type = enumValue.GetType();
            var memInfo = type.GetMember(enumValue.ToString());
            var attributes = memInfo[0].GetCustomAttributes(typeof(EnumMemberAttribute), false);
            return ((EnumMemberAttribute)attributes[0]).Value;
        }
    }
    public enum ComparisonOperator
    {
        [EnumMember(Value = "")]
        None,
        [EnumMember(Value = ">")]
        GreaterThan,
        [EnumMember(Value = "<")]
        LessThan,
        [EnumMember(Value = ">=")]
        GreaterThanOrEqual,
        [EnumMember(Value = "<=")]
        LessThanOrEqual,
        Equal
    }

    [DebuggerDisplay("{VersionArray[3]}.{VersionArray[2]}.{VersionArray[1]}.{VersionArray[0]}")]
    public class Version : IEqualityComparer<Version>
    {
        #region PARTS
        public byte major { get => VersionArray[3]; set => VersionArray[3] = value; }
        public byte minor { get => VersionArray[2]; set => VersionArray[2] = value; }
        public byte patch { get => VersionArray[1]; set => VersionArray[1] = value; }
        public byte revision { get => VersionArray[0]; set => VersionArray[0] = value; }
        #endregion

        private byte[] VersionArray = new byte[4];
        public UInt32 VersionInt { get => GetVersion(); set => SetVersion(value); }

        #region SETTERS
        public void SetVersion(UInt32 version) => VersionArray = BitConverter.GetBytes(version);
        public void SetMajor(byte major) => this.major = major;
        public void SetMinor(byte minor) => this.minor = minor;
        public void SetPatch(byte patch) => this.patch = patch;
        public void SetRevision(byte revision) => this.revision = revision;
        #endregion

        #region GETTERS
        public UInt32 GetVersion() => BitConverter.ToUInt32(VersionArray, 0);

        public byte GetMajor() => this.major;
        public byte GetMinor() => this.minor;
        public byte GetPatch() => this.patch;
        public byte GetRevision() => this.revision;

        #endregion

        #region OPERATORS
        static public bool operator >(Version a, Version b) => a.VersionInt > b.VersionInt;
        static public bool operator <(Version a, Version b) => a.VersionInt < b.VersionInt;
        static public bool operator >=(Version a, Version b) => a.VersionInt >= b.VersionInt;
        static public bool operator <=(Version a, Version b) => a.VersionInt <= b.VersionInt;

        #region EQUALS
        public bool Equals(Version? x, Version? y)
        {
            if (x == null || y == null) return false;
            return x.GetVersion() == y.GetVersion();
        }

        public int GetHashCode([DisallowNull] Version obj)
        {
            return obj.VersionInt.GetHashCode();
        }
        #endregion
        #endregion
        public override string ToString()
        {
            return $"{major}.{minor}.{patch}.{revision}";
        }
        public Version(string versionString)
        {
            string[] versionSections = versionString.Split("-")[0].Split(".");

            for (int i = 0; i < versionSections.Length; i++)
            {
                VersionArray[i] = byte.Parse(versionSections[i]);
            }
            VersionArray = VersionArray.Reverse().ToArray();//we need to reverse because we put the numbers in backwards
        }
    }
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    internal class VersionRange
    {
        private string DebuggerDisplay
        {
            get
            {
                string startPart = $"{EnumHelper.GetEnumValue(OperatorStart)}{start}";

                // Only add the end part if 'end' is not null or empty
                string endPart = !string.IsNullOrEmpty(end?.ToString())
                    ? $":{EnumHelper.GetEnumValue(OperatorEnd)}{end}"
                    : string.Empty;

                return $"{startPart}{endPart}";
            }
        }
        Version? start { get; set; }//start of the range
        Version? end { get; set; }//end of the range
        ComparisonOperator OperatorStart { get; set; }
        ComparisonOperator OperatorEnd { get; set; }
        public VersionRange(ComparisonOperator OperatorStart, Version versionStart)
        {
            this.OperatorStart = OperatorStart;
            start = versionStart;
        }
        public VersionRange(string OperatorStart, Version versionStart) : this(ParseOperator(OperatorStart), versionStart) { }
        public VersionRange(ComparisonOperator OperatorStart, Version versionStart, ComparisonOperator OperatorEnd, Version versionEnd)
        {
            this.OperatorStart = OperatorStart;
            start= versionStart;
            this.OperatorEnd = OperatorEnd;
            end = versionEnd;
        }
        public VersionRange(string OperatorStart, Version versionStart, string OperatorEnd, Version versionEnd) : this(ParseOperator(OperatorStart), versionStart, ParseOperator(OperatorEnd), versionEnd) { }

        public static ComparisonOperator ParseOperator(string op) => op switch
        {
            ">" => ComparisonOperator.GreaterThan,
            "<" => ComparisonOperator.LessThan,
            ">=" => ComparisonOperator.GreaterThanOrEqual,
            "<=" => ComparisonOperator.LessThanOrEqual,
            "==" => ComparisonOperator.Equal,
            _ => ComparisonOperator.None
        };
    }
    internal class VersionUtils
    {
        /// <summary>
        /// This function generates a VersionRange
        /// </summary>
        /// <param name="range"></param>
        /// <returns></returns>
        public static VersionRange GetRangeFromString(string range)
        {
            string[] splitRange = range.Split(":");
            string? versionStart = splitRange.ElementAtOrDefault(0);
            string? versionEnd = splitRange.ElementAtOrDefault(1);
            string versionPattern = @"(\d(\.\d)+)";
            string comparatorPattern = @"[><=!]+(?=\d)";

            if(versionStart != null)
            {
                Version versionBegin = new Version(Regex.Matches(versionStart ?? "0.0.0.0", versionPattern).ElementAtOrDefault(0)!.Value);
                string operatorBegin = Regex.Matches(versionStart ?? "0.0.0.0", comparatorPattern).ElementAtOrDefault(0)!.Value;
                if (versionEnd != null)
                {
                    Version versionLimit = new Version(Regex.Matches(versionEnd ?? "0.0.0.0", versionPattern).ElementAtOrDefault(0)!.Value);
                    string operatorLimit = Regex.Matches(versionEnd ?? "0.0.0.0", comparatorPattern).ElementAtOrDefault(0)!.Value;
                }
                return new VersionRange(operatorBegin, versionBegin);
            }

            return new VersionRange(ComparisonOperator.LessThan,new Version("3.1.1"));
        }
    }
}

