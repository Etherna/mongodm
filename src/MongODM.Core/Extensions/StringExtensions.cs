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
