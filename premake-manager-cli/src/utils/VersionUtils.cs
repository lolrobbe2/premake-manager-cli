using Octokit;
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
    public enum ComparisonOperator : byte
    {
        [EnumMember(Value = "")]
        None = 0,
        [EnumMember(Value = "=")]
        Equal = 1,
        [EnumMember(Value = ">")]
        GreaterThan = 2,
        [EnumMember(Value = ">=")]
        GreaterThanOrEqual = 3,
        [EnumMember(Value = "<")]
        LessThan = 4,
        [EnumMember(Value = "<=")]
        LessThanOrEqual = 5,
        
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
        static public bool operator ==(Version a, Version b) => a.VersionInt == b.VersionInt;
        static public bool operator !=(Version a, Version b) => a.VersionInt != b.VersionInt;

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
}

