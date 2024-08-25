// Copyright 2020-present Etherna SA
// This file is part of MongODM.
// 
// MongODM is free software: you can redistribute it and/or modify it under the terms of the
// GNU Lesser General Public License as published by the Free Software Foundation,
// either version 3 of the License, or (at your option) any later version.
// 
// MongODM is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
// without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public License along with MongODM.
// If not, see <https://www.gnu.org/licenses/>.

using System.Globalization;

namespace Etherna.MongODM.Core.Extensions
{
    public static class StringExtensions
    {
        public static string ToLowerFirstChar(this string str, CultureInfo? cultureInfo = null) =>
            string.IsNullOrEmpty(str) || char.IsLower(str[0]) ? str :
                char.ToLower(str[0], cultureInfo ?? CultureInfo.InvariantCulture) + str.Substring(1);
    }
}
