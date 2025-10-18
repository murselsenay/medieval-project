
using UnityEngine;

namespace Utilities
{
    public static class Abbrevation
    {
        private static readonly string[] ScoreNames = { "", "K", "M", "B", "T", "aa", "ab", "ac", "ad", "ae", "af", "ag", "ah", "ai", "aj", "ak", "al", "am", "an", "ao", "ap", "aq", "ar", "as", "at", "au", "av", "aw", "ax", "ay", "az", "ba", "bb", "bc", "bd", "be", "bf", "bg", "bh", "bi", "bj", "bk", "bl", "bm", "bn", "bo", "bp", "bq", "br", "bs", "bt", "bu", "bv", "bw", "bx", "by", "bz" };

        public static string AbbreviateNumber(float number)
        {
            if (number < 990f)
                return number == Mathf.Floor(number) ? number.ToString() : number.ToString("F1");

            float amount = number;
            int index = 0;

            while (amount >= 990f && index < ScoreNames.Length - 1)
            {
                amount /= 1000f;
                index++;
            }

            return (amount == Mathf.Floor(amount) ? amount.ToString() : amount.ToString("F1")) + ScoreNames[index];
        }
    }
}
