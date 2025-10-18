using Modules.Economy.Enums;
using Modules.ObjectPoolSystem;
using Modules.Economy.Managers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Modules.EventSystem.Managers;

namespace Components.Currencies.Controllers
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public class CurrencyObjectController : BaseObject
    {
        [SerializeField] private ECurrencyType _currencyType;
        [SerializeField] private int _amount = 1;

        [Header("Drop Settings")]
        [SerializeField] private float _dropRadius = 1f;
        [SerializeField] private float _jumpPower = 0.25f;
        [SerializeField] private int _numJumps = 1;
        [SerializeField] private float _duration = 0.4f;

        [Header("Magnet to UI")]
        [Tooltip("Delay in seconds before the coin/gem starts moving toward the UI target")]
        [SerializeField] private float _magnetDelay = 3.5f;
        [SerializeField] private float _magnetDuration = 0.6f;
        [SerializeField] private Ease _magnetEase = Ease.InQuad;

        [Header("Collection")]
        [Tooltip("Tag of the collector. If empty, any trigger will collect.")]
        [SerializeField] private string _collectorTag = "Player";

        [SerializeField] private Rigidbody _rb;
        [SerializeField] private Collider _coll;

        private Tween _dropTween;
        private Tween _magnetTween;
        private Coroutine _magnetRoutine;

        // Cache base scales to prevent cumulative scaling on UI targets
        private static readonly System.Collections.Generic.Dictionary<int, UnityEngine.Vector3> s_UITargetBaseScales = new System.Collections.Generic.Dictionary<int, UnityEngine.Vector3>();

        protected override void Awake()
        {
            base.Awake();
            if (_rb == null) _rb = GetComponent<Rigidbody>();
            if (_coll == null) _coll = GetComponent<Collider>();
            if (_coll != null) _coll.isTrigger = true;
        }
        public void Initialize(int amount)
        {
            _amount = amount;
        }
        public override void Activate()
        {
            base.Activate();

            transform.localScale = Vector3.one;

            var rends = GetComponentsInChildren<Renderer>(true);
            foreach (var r in rends) r.enabled = true;

            if (_rb != null) _rb.isKinematic = false;
            if (_coll != null) _coll.enabled = false;

            transform.DOKill();

            PlayDropAnimation();
        }

        public override void Deactivate()
        {
            transform.DOKill();
            try { StopAllCoroutines(); } catch { }

            // Reset physics state
            if (_rb != null) _rb.isKinematic = true;
            if (_coll != null) _coll.enabled = false;

            base.Deactivate();
        }

        private void PlayDropAnimation()
        {
            Vector2 rnd = Random.insideUnitCircle * _dropRadius;
            Vector3 target = transform.position + new Vector3(rnd.x, 0f, rnd.y);

            Ray ray = new Ray(target + Vector3.up * 2f, Vector3.down);
            if (Physics.Raycast(ray, out RaycastHit hit, 5f))
            {
                target.y = hit.point.y + 0.02f;
            }
            else
            {
                target.y = transform.position.y;
            }

            float jumpPower = Mathf.Max(0.05f, _jumpPower);

            _dropTween = transform.DOJump(target, jumpPower, Mathf.Max(1, _numJumps), _duration)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    transform.position = target;
                    if (_coll != null) _coll.enabled = true;
                    if (_rb != null) _rb.isKinematic = true;

                    try { _magnetRoutine = StartCoroutine(MagnetToUITargetRoutine()); } catch { }
                });
        }

        private IEnumerator MagnetToUITargetRoutine()
        {
            yield return new WaitForSeconds(_magnetDelay);

            string targetTag = _currencyType.ToString();

            GameObject uiTarget = null;
            try { uiTarget = GameObject.FindWithTag(targetTag); } catch { }

            if (uiTarget == null)
            {
                var rects = GameObject.FindObjectsOfType<RectTransform>();
                for (int i = 0; i < rects.Length; i++)
                {
                    if (rects[i] == null) continue;
                    if (rects[i].name.ToLower().Contains(targetTag.ToLower()))
                    {
                        uiTarget = rects[i].gameObject;
                        break;
                    }
                }
            }

            if (uiTarget == null)
                yield break;

            var rect = uiTarget.GetComponent<RectTransform>();
            Camera cam = Camera.main;
            if (cam == null) yield break;

            Vector3 startScreenPos = cam.WorldToScreenPoint(transform.position);
            if (startScreenPos.z <= 0f) startScreenPos.z = cam.nearClipPlane + 0.5f;

            if (_coll != null) _coll.enabled = false;
            if (_rb != null) _rb.isKinematic = true;

            // Ensure the currency object's scale is correct before the punch
            try { transform.localScale = Vector3.one; } catch { }
            transform.DOPunchScale(Vector3.one * 0.12f, 0.12f, 2, 0.5f);

            float startZ = startScreenPos.z;
            float endZ = Mathf.Clamp(cam.nearClipPlane + 0.75f, 0.15f, startZ);

            float progress = 0f;
            _magnetTween = DOTween.To(() => progress, x =>
            {
                progress = x;

                Vector2 targetSP;
                if (rect != null)
                {
                    Canvas parentCanvas = rect.GetComponentInParent<Canvas>();
                    if (parentCanvas != null && parentCanvas.renderMode == RenderMode.ScreenSpaceCamera)
                    {
                        targetSP = RectTransformUtility.WorldToScreenPoint(parentCanvas.worldCamera, rect.position);
                    }
                    else if (parentCanvas != null && parentCanvas.renderMode == RenderMode.WorldSpace)
                    {
                        targetSP = RectTransformUtility.WorldToScreenPoint(cam, rect.position);
                    }
                    else
                    {
                        targetSP = RectTransformUtility.WorldToScreenPoint(null, rect.position);
                      }
                }
                else
                {
                    Vector3 sp = cam.WorldToScreenPoint(uiTarget.transform.position);
                    targetSP = new Vector2(sp.x, sp.y);
                }

                float z = Mathf.Lerp(startZ, endZ, progress);
                Vector3 screenPos = new Vector3(
                    Mathf.Lerp(startScreenPos.x, targetSP.x, progress),
                    Mathf.Lerp(startScreenPos.y, targetSP.y, progress),
                    z
                );

                Vector3 worldPos = cam.ScreenToWorldPoint(screenPos);
                transform.position = worldPos;

            }, 1f, _magnetDuration)
            .SetEase(_magnetEase)
            .OnComplete(() =>
            {
                try
                {
                    if (uiTarget != null)
                    {
                        var t = uiTarget.transform;
                        // Cache and restore base scale deterministically to avoid cumulative growth
                        int id = t.GetInstanceID();
                        Vector3 baseScale;
                        if (!s_UITargetBaseScales.TryGetValue(id, out baseScale))
                        {
                            baseScale = t.localScale;
                            s_UITargetBaseScales[id] = baseScale;
                        }

                        // Kill any running tweens without completing, then reset to base scale
                        try { t.DOKill(false); } catch { }
                        try { t.localScale = baseScale; } catch { }

                        // Absolute bounce animation around base scale
                        try
                        {
                            float upMul = 1.12f;
                            Sequence bounce = DOTween.Sequence();
                            bounce.Append(t.DOScale(baseScale * upMul, 0.12f).SetEase(Ease.OutQuad));
                            bounce.Append(t.DOScale(baseScale, 0.16f).SetEase(Ease.OutBack));
                        }
                        catch { }
                    }
                }
                catch { }

                EventManager.DelegateCurrencyRequestCompleted(_currencyType);

                try { Deactivate(); } catch { gameObject.SetActive(false); }
            });

            yield break;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other == null) return;
            if (!string.IsNullOrEmpty(_collectorTag) && !other.CompareTag(_collectorTag)) return;

            Collect(other.transform);
        }

        private void Collect(Transform collector)
        {
            CurrencyManager.Add(_currencyType, _amount);

            Sequence seq = DOTween.Sequence();
            seq.Append(transform.DOMove(collector.position, 0.18f).SetEase(Ease.InQuad));
            seq.Join(transform.DOScale(Vector3.zero, 0.18f).SetEase(Ease.InBack));
            seq.OnComplete(() =>
            {
                try { Deactivate(); } catch { gameObject.SetActive(false); }
            });
        }
    }
}
