using System.Text;
using Modules.Logger;

namespace Utilities.Extensions
{
    public static class ByteExtensions
    {
        public static byte[] ToBytes(this string str)
        {
            return Encoding.UTF8.GetBytes(str);
        }

        public static string FromBytes(this byte[] bytes)
        {
            try
            {
                return Encoding.UTF8.GetString(bytes);
            }
            catch (System.Exception ex)
            {
                DebugLogger.LogError($"Error converting bytes to string: {ex.Message}");
                return string.Empty;
            }
        }
    }
}
