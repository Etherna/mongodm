using System;
using System.Text;
using System.Text.RegularExpressions;

namespace Etherna.MongODM.Serialization
{
    public class DocumentVersion : IComparable<DocumentVersion>
    {
        // Constructors.
        /// <summary>
        /// Construct from string version
        /// </summary>
        /// <param name="version">The version as string (ex. 3.1.4-alpha1)</param>
        public DocumentVersion(string version)
        {
            // Accepted formats for version:
            // * 3
            // * 3.1
            // * 3.1.4
            // * 3.1.4-alpha1
            // * 3.1.4-beta.2
            // * 3.1-DEV
            var match = Regex.Match(version,
                @"^(?<major>\d+)(\.(?<minor>\d+))?(\.(?<patch>\d+))?(-(?<label>[A-Z0-9.-]+))?$",
                RegexOptions.IgnoreCase);
            var majorGroup = match.Groups["major"];
            var minorGroup = match.Groups["minor"];
            var patchGroup = match.Groups["patch"];
            var labelGroup = match.Groups["label"];

            MajorRelease = int.Parse(majorGroup.Value);
            if (minorGroup.Success)
                MinorRelease = int.Parse(minorGroup.Value);
            if (patchGroup.Success)
                PatchRelease = int.Parse(patchGroup.Value);
            if (labelGroup.Success)
                LabelRelease = labelGroup.Value;
        }

        /// <summary>
        /// Construct from values
        /// </summary>
        /// <param name="major">Major version</param>
        /// <param name="minor">Minor version</param>
        /// <param name="patch">Patch version</param>
        /// <param name="label">Additional label</param>
        public DocumentVersion(
            int major,
            int minor,
            int patch,
            string? label)
        {
            MajorRelease = major;
            MinorRelease = minor;
            PatchRelease = patch;
            LabelRelease = label;
        }

        // Properties.
        public int MajorRelease { get; private set; }
        public int MinorRelease { get; private set; }
        public int PatchRelease { get; private set; }
        public string? LabelRelease { get; private set; }

        // Overrides.
        public int CompareTo(DocumentVersion? other)
        {
            // If other is not a valid object reference, this instance is greater.
            if (other is null) return 1;

            if (this > other) return 1;
            if (this == other) return 0;
            else return -1;
        }

        public override bool Equals(object obj) => this == (obj as DocumentVersion);

        public override int GetHashCode()
        {
            var hash = MajorRelease.GetHashCode() ^
                MinorRelease.GetHashCode() ^
                PatchRelease.GetHashCode();
            if (LabelRelease != null)
            {
                hash ^= LabelRelease.GetHashCode();
            }
            return hash;
        }

        public override string ToString()
        {
            var strBuilder = new StringBuilder($"{MajorRelease}.{MinorRelease}.{PatchRelease}");
            if (LabelRelease != null)
            {
                strBuilder.Append($"-{LabelRelease}");
            }
            return strBuilder.ToString();
        }

        // Operators.
        public static bool operator < (DocumentVersion? x, DocumentVersion? y)
        {
            // Check if null.
            if (y == null)
                return false;
            else if (x == null) //y != null
                return true;

            // Check major release.
            if (x.MajorRelease != y.MajorRelease)
                return x.MajorRelease < y.MajorRelease;

            // Check minor release.
            if (x.MinorRelease != y.MinorRelease)
                return x.MinorRelease < y.MinorRelease;

            // Check patch release.
            if (x.PatchRelease != y.PatchRelease)
                return x.PatchRelease < y.PatchRelease;

            return false;
        }

        public static bool operator > (DocumentVersion? x, DocumentVersion? y) => y < x;

        public static bool operator == (DocumentVersion? x, DocumentVersion? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null) return false;

            return x.MajorRelease == y.MajorRelease &&
                x.MinorRelease == y.MinorRelease &&
                x.PatchRelease == y.PatchRelease &&
                x.LabelRelease == y.LabelRelease;
        }

        public static bool operator != (DocumentVersion x, DocumentVersion y) => !(x == y);

        public static implicit operator DocumentVersion(string version) => new DocumentVersion(version);
    }
}
