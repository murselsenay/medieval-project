using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
using Scriptables.Singletons;
using NaughtyAttributes;
using TMPro;
using UnityEngine.Events;

namespace Utilities
{
    [RequireComponent(typeof(Button))]
    public class ButtonAnimate : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private Button _button;
        [SerializeField] private TMP_Text _buttonText;
        [Header("Animation")]
        [SerializeField] private float _pressedScale = 0.95f;
        [SerializeField] private float _animDuration = 0.08f;

        [Header("TMP Settings")]
        [SerializeField] private Color _disabledTextColor = new Color(0.45f, 0.45f, 0.45f, 1f);

        private Vector3 _originalScale;
        private Tween _currentTween;
        private bool _isActive = true;

        // Store original TMP colors so we can restore them on Activate
        private readonly Dictionary<TextMeshProUGUI, Color> _originalTmpColors = new Dictionary<TextMeshProUGUI, Color>();

        private void OnValidate()
        {
            // Try to auto-assign Button if not set (editor-time)
            if (_button == null)
                _button = GetComponent<Button>();
        }

        private void Awake()
        {
            if (_button == null)
                _button = GetComponent<Button>();

            _originalScale = transform.localScale;
        }

        public void SetText(string text) => _buttonText.text = text;
        public void OnPointerDown(PointerEventData eventData)
        {
            // Only animate if active (interactable)
            if (!_isActive || (_button != null && !_button.interactable)) return;

            _currentTween?.Kill();
            _currentTween = transform.DOScale(_originalScale * _pressedScale, _animDuration).SetUpdate(true);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_isActive || (_button != null && !_button.interactable)) return;

            _currentTween?.Kill();
            _currentTween = transform.DOScale(_originalScale, _animDuration).SetUpdate(true);
        }

        [Button]
        public void Deactivate()
        {
            if (!_isActive) return;
            _isActive = false;

            ApplyGrayscaleToImages();
            ApplyGrayscaleToTMPTexts();

            if (_button != null)
                _button.interactable = false;
        }
        [Button]
        public void Activate()
        {
            if (_isActive) return;
            _isActive = true;

            ClearMaterialFromImages();
            RestoreTMPTextColors();

            if (_button != null)
                _button.interactable = true;
        }

        // Adds a listener to the underlying Button.onClick
        public void AddListener(UnityAction action)
        {
            if (_button == null)
                _button = GetComponent<Button>();

            if (_button == null || action == null) return;
            _button.onClick.AddListener(action);
        }

        // Removes all listeners from the underlying Button.onClick
        public void RemoveAllListeners()
        {
            if (_button == null)
                _button = GetComponent<Button>();

            if (_button == null) return;
            _button.onClick.RemoveAllListeners();
        }

        private void ApplyGrayscaleToImages()
        {
            var grayMat = ResourceWarehouse.Instance != null ? ResourceWarehouse.Instance.GrayscaleUIMaterial : null;
            if (grayMat == null) return;

            var images = GetComponentsInChildren<Image>(true);
            foreach (var img in images)
            {
                // Use material, not sharedMaterial, so we don't modify project assets. It's fine for UI
                img.material = grayMat;
            }
        }

        private void ClearMaterialFromImages()
        {
            var images = GetComponentsInChildren<Image>(true);
            foreach (var img in images)
            {
                img.material = null;
            }
        }

        private void ApplyGrayscaleToTMPTexts()
        {
            var tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var tmp in tmps)
            {
                if (tmp == null) continue;

                // Store original color if we haven't already
                if (!_originalTmpColors.ContainsKey(tmp))
                    _originalTmpColors[tmp] = tmp.color;

                // Preserve original alpha, but set to disabled color RGB
                var alpha = tmp.color.a;
                tmp.color = new Color(_disabledTextColor.r, _disabledTextColor.g, _disabledTextColor.b, alpha);
            }
        }

        private void RestoreTMPTextColors()
        {
            foreach (var kv in _originalTmpColors)
            {
                var tmp = kv.Key;
                var originalColor = kv.Value;

                if (tmp == null) continue;
                tmp.color = originalColor;
            }

            _originalTmpColors.Clear();
        }
    }
}