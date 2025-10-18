using TMPro;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace Modules.Utilities.Extensions
{
    public static class TextMeshProExtensions
    {
        public static async UniTask UpdateTextWithAnimation(this TextMeshProUGUI textComponent, int from, int to, float duration)
        {
            int currentValue = from;
            await DOTween.To(() => currentValue, x =>
            {
                currentValue = x;
                textComponent.text = currentValue.ToString();
            }, to, duration).AsyncWaitForCompletion();
        }
    }
}
