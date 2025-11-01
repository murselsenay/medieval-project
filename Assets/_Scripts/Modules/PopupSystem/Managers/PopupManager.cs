using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Modules.AdressableSystem;
using Modules.PopupSystem.Components;
using Modules.EventSystem.Managers;
using Modules.PopupSystem.Enums;
using UnityEngine;

namespace Modules.PopupSystem.Managers
{
    public static class PopupManager
    {
        private static readonly Queue<EPopup> _queue = new Queue<EPopup>();
        private static BasePopup _currentPopup;
        private static EPopup _currentPopupType = EPopup.None;
        private static bool _isProcessing = false;
        private static GameObject _popupHolder;

        public static void ShowPopup(EPopup popup)
        {
            if (popup == EPopup.None) return;

            // If this popup is already open, ignore the request
            if (_currentPopupType == popup) return;

            // If this popup is already queued, ignore duplicate request
            if (_queue.Contains(popup)) return;

            _queue.Enqueue(popup);
            ProcessQueue().Forget();
        }

        private static async UniTaskVoid ProcessQueue()
        {
            if (_isProcessing) return;
            _isProcessing = true;

            while (_queue.Count > 0)
            {
                var popup = _queue.Dequeue();
                await ShowSingleAsync(popup);
            }

            _isProcessing = false;
        }

        private static void EnsurePopupHolder()
        {
            if (_popupHolder == null)
            {
                _popupHolder = GameObject.Find("Popup_Holder");
                if (_popupHolder == null)
                {
                    _popupHolder = new GameObject("Popup_Holder");
                    Object.DontDestroyOnLoad(_popupHolder);
                }
            }
        }

        private static async UniTask ShowSingleAsync(EPopup popup)
        {
            EnsurePopupHolder();
            string key = $"popup-{popup.ToString().ToLowerInvariant()}";

            BasePopup instance = await AddressableManager.InstantiateAsync<BasePopup>(key, _popupHolder.transform);
            if (instance == null) return;

            _currentPopup = instance;
            _currentPopupType = popup;

            var tcs = new UniTaskCompletionSource<bool>();

            void OnClosed(BasePopup b)
            {
                if (b == _currentPopup)
                    tcs.TrySetResult(true);
            }

            EventManager.OnPopupClosed += OnClosed;

            try
            {
                instance.Activate();
            }
            catch { }

            await tcs.Task;

            EventManager.OnPopupClosed -= OnClosed;

            if (instance != null && instance.gameObject != null)
            {
                AddressableManager.ReleaseInstance(instance.gameObject);
            }

            _currentPopup = null;
            _currentPopupType = EPopup.None;
        }
    }
}