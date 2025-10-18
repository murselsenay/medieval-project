using DG.Tweening;
using UnityEngine;

namespace Utilities.Extensions
{
    public static class DOTweenExtensions
    {
        public static Tween KillPrevious(this Tween tween)
        {
            if (tween != null && tween.IsActive() && tween.IsPlaying())
            {
                tween.Kill();
            }

            return tween;
        }

        public static T KillPreviousTweens<T>(this T component) where T : Component
        {
            if (component != null)
            {
                var tweens = DOTween.TweensByTarget(component);
                if (tweens.IsNullOrEmpty()) return component;

                foreach (var tween in tweens)
                {
                    tween.Kill();
                }
            }

            return component;

        }
    }
}
