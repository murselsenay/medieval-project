using System;
using System.Collections.Generic;

namespace Utilities.Extensions
{
    public static class ListExtensions
    {
        private static readonly Random _random = new();

        public static bool IsNullOrEmpty<T>(this List<T> list)
        {
            return list == null || list.Count == 0;
        }

        public static void Shuffle<T>(this List<T> list)
        {
            Random rng = new();
            int n = list.Count;
            while (n > 1)
            {
                int k = rng.Next(n--);
                (list[n], list[k]) = (list[k], list[n]);
            }
        }

        public static T Random<T>(this List<T> list)
        {
            if (IsNullOrEmpty(list))
            {
                throw new InvalidOperationException("Cannot select a random element from an empty list.");
            }

            int index = _random.Next(list.Count);
            return list[index];
        }
    }
}
