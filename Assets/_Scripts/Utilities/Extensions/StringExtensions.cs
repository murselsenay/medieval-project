using System.Text;

namespace Utilities.Extensions
{
    public static class StringExtensions
    {
        public static bool IsNullOrEmpty(this string str)
        {
            return string.IsNullOrEmpty(str);
        }

        public static string ToKebabCase(this string str)
        {
            if (string.IsNullOrEmpty(str))
                return str;

            var stringBuilder = new StringBuilder();
            for (int i = 0; i < str.Length; i++)
            {
                char c = str[i];
                if (char.IsUpper(c) && i > 0)
                {
                    stringBuilder.Append('-');
                }
                stringBuilder.Append(char.ToLower(c));
            }
            return stringBuilder.ToString();
        }

        public static string ToEmptySeperated(this string str)
        {
            if (string.IsNullOrEmpty(str))
                return str;

            var stringBuilder = new StringBuilder();
            foreach (char c in str)
            {
                if (char.IsUpper(c) && stringBuilder.Length > 0)
                {
                    stringBuilder.Append(' ');
                }
                stringBuilder.Append(c);
            }
            return stringBuilder.ToString();
        }

        public static string Shorten(this string s, int count)
        {
            if (string.IsNullOrEmpty(s))
                return s;

            if (s.Length <= count)
                return s;

            return s[..count] + "..";
        }
    }
}
