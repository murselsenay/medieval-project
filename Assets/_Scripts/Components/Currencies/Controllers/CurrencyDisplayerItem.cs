using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Modules.Economy.Enums;
using Modules.Economy.Managers;
using Scriptables.Singletons;
using Modules.EventSystem.Managers;

namespace Components.Currencies.Controllers
{
    public class CurrencyDisplayerItem : MonoBehaviour
    {
        [SerializeField] private TMP_Text _amountText;
        [SerializeField] private Image _iconImage;

        [Header("Settings")]
        [SerializeField] private ECurrencyType _currencyType = ECurrencyType.Gold;
        [SerializeField] private bool _useThousandShortening = true;

        private void OnEnable()
        {
            EventManager.OnCurrencyRequestCompleted += OnCurrencyChanged;
            RefreshAll();
        }

        private void OnDisable()
        {
            EventManager.OnCurrencyRequestCompleted -= OnCurrencyChanged;
        }
        private void OnCurrencyChanged(ECurrencyType type)
        {
            if (type != _currencyType) return;
            ApplyAmount(CurrencyManager.GetAmount(_currencyType));
        }

        private void RefreshAll()
        {
            ApplyIcon();
            ApplyAmount(CurrencyManager.GetAmount(_currencyType));
        }

        private void ApplyIcon()
        {
            if (_iconImage == null) return;
            var sprite = ResourceWarehouse.Instance != null ? ResourceWarehouse.Instance.GetCurrencySprite(_currencyType) : null;
            _iconImage.sprite = sprite;
            _iconImage.enabled = sprite != null;
        }

        private void ApplyAmount(int amount)
        {
            if (_amountText == null) return;
            _amountText.text = _useThousandShortening ? Shorten(amount) : amount.ToString();
        }

        private static string Shorten(int value)
        {
            if (value >= 1_000_000_000) return (value / 1_000_000_000f).ToString("0.#") + "B";
            if (value >= 1_000_000) return (value / 1_000_000f).ToString("0.#") + "M";
            if (value >= 1_000) return (value / 1_000f).ToString("0.#") + "K";
            return value.ToString();
        }
    }
}