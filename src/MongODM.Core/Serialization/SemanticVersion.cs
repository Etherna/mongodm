//   Copyright 2020-present Etherna Sagl
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.

using MongoDB.Bson;
using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Etherna.MongODM.Core.Serialization
{
    public class SemanticVersion : IComparable<SemanticVersion>
    {
        // Constructors.
        /// <summary>
        /// Construct from string version
        /// </summary>
        /// <param name="version">The version as string (ex. 3.1.4-alpha1)</param>
        public SemanticVersion(string version)
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

            MajorRelease = int.Parse(majorGroup.Value, CultureInfo.InvariantCulture);
            if (minorGroup.Success)
                MinorRelease = int.Parse(minorGroup.Value, CultureInfo.InvariantCulture);
            if (patchGroup.Success)
                PatchRelease = int.Parse(patchGroup.Value, CultureInfo.InvariantCulture);
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
        public SemanticVersion(
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

        // Methods.
        public BsonArray ToBsonArray()
        {
            var bsonArray = new BsonArray(new[]
            {
                new BsonInt32(MajorRelease),
                new BsonInt32(MinorRelease),
                new BsonInt32(PatchRelease)
            });
            if (LabelRelease != null)
            {
                bsonArray.Add(new BsonString(LabelRelease));
            }

            return bsonArray;
        }

        // Overrides.
        public int CompareTo(SemanticVersion? other)
        {
            // If other is not a valid object reference, this instance is greater.
            if (other is null) return 1;

            if (this < other) return -1;
            if (this == other) return 0;
            else return 1;
        }

        public override bool Equals(object obj) => this == (obj as SemanticVersion);

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
        public static bool operator < (SemanticVersion? x, SemanticVersion? y)
        {
            // Check if null.
            if (y is null)
                return false;
            else if (x is null) //y != null
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

        public static bool operator > (SemanticVersion? x, SemanticVersion? y) => y < x;

        public static bool operator == (SemanticVersion? x, SemanticVersion? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null) return false;

            return x.MajorRelease == y.MajorRelease &&
                x.MinorRelease == y.MinorRelease &&
                x.PatchRelease == y.PatchRelease &&
                x.LabelRelease == y.LabelRelease;
        }

        public static bool operator != (SemanticVersion x, SemanticVersion y) => !(x == y);

        public static bool operator <= (SemanticVersion x, SemanticVersion y) => x < y || x == y;

        public static bool operator >=(SemanticVersion x, SemanticVersion y) => y <= x;

        public static implicit operator SemanticVersion(string version) => new SemanticVersion(version);
    }
}
