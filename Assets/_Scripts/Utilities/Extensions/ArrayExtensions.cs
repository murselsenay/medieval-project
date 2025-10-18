using System;

namespace Utilities.Extensions
{
    public static class ArrayExtensions
    {
        public static bool IsNullOrEmpty<T>(this T[] array)
        {
            return array == null || array.Length == 0;
        }

        public static void Shuffle<T>(this T[] array)
        {
            Random rng = new();
            int n = array.Length;
            while (n > 1)
            {
                int k = rng.Next(n--);
                (array[n], array[k]) = (array[k], array[n]);
            }
        }
    }
}
