using System.Collections.Generic;

namespace Utilities.Extensions
{
    public static class IEnumerableExtensions
    {
        public static bool IsNullOrEmpty<T>(this IEnumerable<T> enumerable)
        {
            return enumerable == null || !enumerable.GetEnumerator().MoveNext();
        }
    }
}
