using Modules.ObjectPoolSystem;
using TMPro;
using UnityEngine;
using Utilities;
using Modules.EventSystem.Managers;
using UnityEngine.UI;
using DG.Tweening;
using Modules.AdressableSystem;

namespace Modules.PopupSystem.Components
{
    public class BasePopup : BaseObject
    {
        [BHeader("Popup Components")]
        [SerializeField] private Image _blackBackground;
        [SerializeField] private RectTransform _subholder;
        [SerializeField] private TMP_Text _headerText;
        [SerializeField] private ButtonAnimate _button;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _backgroundButton;

        [SerializeField] private float _fadeDuration = 0.2f;
        [SerializeField] private float _scaleDuration = 0.35f;
        [SerializeField] private float _targetAlpha = 0.7f;

        private Sequence _sequence;
        private bool _isClosing = false;

        protected override void Awake()
        {
            base.Awake();
            if (_blackBackground != null)
            {
                var c = _blackBackground.color;
                c.a = 0f;
                _blackBackground.color = c;
            }
            if (_subholder != null)
                _subholder.localScale = Vector3.zero;

            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveAllListeners();
                _closeButton.onClick.AddListener(() => Close(true));
            }

            if (_backgroundButton != null)
            {
                _backgroundButton.onClick.RemoveAllListeners();
                _backgroundButton.onClick.AddListener(() => Close(true));
            }
            else if (_blackBackground != null)
            {
                var existing = _blackBackground.GetComponent<Button>();
                if (existing != null)
                {
                    existing.onClick.RemoveAllListeners();
                    existing.onClick.AddListener(() => Close(true));
                }
            }
        }
        public virtual void Init() { }
        public override void Activate()
        {
            base.Activate();
            _sequence?.Kill();
            _sequence = DOTween.Sequence().SetUpdate(true);

            if (_blackBackground != null)
                _sequence.Append(_blackBackground.DOFade(_targetAlpha, _fadeDuration));

            if (_subholder != null)
                _sequence.Append(_subholder.DOScale(Vector3.one, _scaleDuration).SetEase(Ease.OutBack));

            _sequence.OnComplete(() =>
            {
                try { OnAfterShow(); } catch { }
                EventManager.DelegatePopupShowed(this);
            });

            Init();
        }

        // Hook that derived popups can override to cleanup AFTER close animation completes
        protected virtual void OnAfterClose() { }

        // Hook that derived popups can override to react AFTER show animation completes
        protected virtual void OnAfterShow() { }

        public override void Deactivate()
        {
            if (_isClosing) return;

            _isClosing = true;

            _sequence?.Kill();
            _sequence = DOTween.Sequence().SetUpdate(true);

            if (_subholder != null)
                _sequence.Append(_subholder.DOScale(Vector3.zero, _scaleDuration).SetEase(Ease.InBack));

            if (_blackBackground != null)
                _sequence.Append(_blackBackground.DOFade(0f, _fadeDuration));

            _sequence.OnComplete(() =>
            {
                // allow derived classes to cleanup pooled children AFTER close animation
                try { OnAfterClose(); } catch { }

                base.Deactivate();
                EventManager.DelegatePopupClosed(this);
                _isClosing = false;
            });
        }

        public void Close(bool releaseInstance = true)
        {
            if (_isClosing) return;

            _isClosing = true;

            _sequence?.Kill();
            _sequence = DOTween.Sequence().SetUpdate(true);

            if (_subholder != null)
                _sequence.Append(_subholder.DOScale(Vector3.zero, _scaleDuration).SetEase(Ease.InBack));

            if (_blackBackground != null)
                _sequence.Append(_blackBackground.DOFade(0f, _fadeDuration));

            _sequence.OnComplete(() =>
            {
                // allow derived classes to cleanup pooled children AFTER close animation
                try { OnAfterClose(); } catch { }

                try
                {
                    base.Deactivate();
                }
                catch { }
                EventManager.DelegatePopupClosed(this);

                if (releaseInstance)
                {
                    if (this.gameObject != null)
                        AddressableManager.ReleaseInstance(this.gameObject);
                }
                else
                {
                    if (this.gameObject != null)
                        UnityEngine.Object.Destroy(this.gameObject);
                }

                _isClosing = false;
            });
        }
    }
}