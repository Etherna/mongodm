using System.Collections.Generic;

namespace Etherna.MongODM.Core.Extensions
{
    public static class ExtraElementsExtensions
    {
        public static TValue? TryGetExtraElementValue<TValue>(this IDictionary<string, object>? extraElements, string key) =>
            extraElements is not null &&
                extraElements.TryGetValue(key, out var objValue) &&
                objValue is TValue value ?
            value :
            default;
    }
}
