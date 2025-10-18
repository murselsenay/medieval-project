using UnityEngine;

namespace Utilities.Extensions
{
    public static class TransformExtensions
    {
        public static bool TryGetComponentInChildren<T>(this Transform transform, out T component) where T : class
        {
            component = transform.GetComponentInChildren<T>();
            return component != null;
        }
    }
}
