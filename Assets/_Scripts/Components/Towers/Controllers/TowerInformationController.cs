using Components.Towers.Models;
using Modules.TowerSystem.Managers;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using Modules.EventSystem.Managers;
using Utilities;
using UnityEngine.UI;
using Modules.Economy.Enums;
using Modules.Economy.Managers;

namespace Components.Towers.Controllers
{
    public class TowerInformationController : MonoBehaviour
    {
        [Header("Content")]
        [SerializeField] private TMP_Text _towerNameText;
        [SerializeField] private TMP_Text _towerRangeText;
        [SerializeField] private TMP_Text _towerHealthText;
        [SerializeField] private TMP_Text _towerDamageText;

        [Header("Positioning / Behavior")]
        [SerializeField] private Canvas _canvas;
        [SerializeField] private RectTransform _panelRect;
        [SerializeField] private Vector3 _worldOffset = new Vector3(0f, 1.5f, 0f);

        [Header("Upgrade")]
        [SerializeField] private ButtonAnimate _upgradeButton;
        [SerializeField] private TMP_Text _upgradeButtonText;

        [Header("Force Layout Rects")]
        [SerializeField] private RectTransform[] _layoutRects;
        [SerializeField] private float _hideDebounceSeconds = 0.25f;
        private float _ignoreHideUntil = 0f;
        private int _towerId;
        private BaseTower _currentTower;

        private void Awake()
        {
            if (_panelRect == null)
                _panelRect = GetComponentInChildren<RectTransform>();

            ToggleContent(false);
        }

        private void Update()
        {
            if (!UnityEngine.Input.GetMouseButtonDown(0))
                return;

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            if (Time.realtimeSinceStartup < _ignoreHideUntil)
                return;

            if (_panelRect != null && RectTransformUtility.RectangleContainsScreenPoint(_panelRect, UnityEngine.Input.mousePosition, null))
                return;

            Hide();
        }
        private void UpdateLayouts()
        {
            if (_layoutRects == null) return;
            foreach (var item in _layoutRects)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(item);
            }
        }
        private void SetUpgrade()
        {
            _upgradeButtonText.text = $"Upgrade <sprite=0>{TowerManager.GetNextUpgradeCost(_towerId)}";
            _upgradeButton.RemoveAllListeners();
            _upgradeButton.AddListener(() =>
            {
                TowerManager.UpgradeTower(_towerId);
                Hide();
            });

            OnCurrencyChanged(ECurrencyType.Gold, CurrencyManager.GetAmount(ECurrencyType.Gold));
        }
        private void OnTowerClicked(int towerId)
        {
            _towerId = towerId;

            var towers = GameObject.FindGameObjectsWithTag(Components.Constants.GameObjectTags.Tower);
            if (towers == null || towers.Length == 0)
            {
                Hide();
                return;
            }

            SetUpgrade();

            GameObject found = null;
            foreach (var t in towers)
            {
                if (t == null) continue;
                if (t.GetInstanceID() == towerId)
                {
                    found = t;
                    break;
                }
            }

            if (found == null)
            {
                Hide();
                return;
            }

            var tower = found.GetComponent<BaseTower>();
            if (tower == null)
            {
                Hide();
                return;
            }

            ShowForTower(tower);

            _ignoreHideUntil = Time.realtimeSinceStartup + _hideDebounceSeconds;

            UpdateLayouts();
        }

        public void Initialize(BaseTower tower)
        {
            if (tower == null) return;

            int towerId = tower.TowerId;
            var info = TowerManager.GetCurrentLevelInfo(towerId);
            var nextInfo = TowerManager.GetNextLevelInfo(towerId);

            if (_towerRangeText != null)
                _towerRangeText.text = $"{info.Data.ProjectileRange}(+{nextInfo.Data.ProjectileRange - info.Data.ProjectileRange})";

            if (_towerHealthText != null)
                _towerHealthText.text = $"{info.Data.Health}(+{nextInfo.Data.Health - info.Data.Health})";

            if (_towerDamageText != null)
                _towerDamageText.text = $"{info.DPS}(+{nextInfo.DPS - info.DPS})";

            if (_towerNameText != null)
                _towerNameText.text = info.Data.TowerType.ToString();
        }

        public void ShowForTower(BaseTower tower)
        {
            if (tower == null || _panelRect == null || _canvas == null) return;

            // Hide range visuals on all towers first to ensure no stale visuals remain
            var towers = GameObject.FindGameObjectsWithTag(Components.Constants.GameObjectTags.Tower);
            if (towers != null && towers.Length > 0)
            {
                foreach (var t in towers)
                {
                    if (t == null) continue;
                    var bt = t.GetComponent<BaseTower>();
                    if (bt == null) continue;
                    if (bt != tower)
                    {
                        bt.SetRangeVisible(false);
                    }
                }
            }

            _currentTower = tower;

            Initialize(tower);

            ToggleContent(true);

            // show tower range visual
            _currentTower.SetRangeVisible(true);

            Vector3 worldPos = tower.transform.position + _worldOffset;
            var cam = Camera.main;
            Vector3 screenPoint = (cam != null) ? cam.WorldToScreenPoint(worldPos) : Vector3.zero;

            RectTransform canvasRect = _canvas.GetComponent<RectTransform>();
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, null, out localPoint);
            _panelRect.anchoredPosition = localPoint;
        }

        public void Hide()
        {
            // hide range visual when panel hides
            if (_currentTower != null)
            {
                _currentTower.SetRangeVisible(false);
                _currentTower = null;
            }

            ToggleContent(false);
        }

        private void ToggleContent(bool active)
        {
            if (_panelRect == null) return;
            _panelRect.gameObject.SetActive(active);
        }

        private void OnCurrencyChanged(ECurrencyType type, int amount)
        {
            if (type != ECurrencyType.Gold) return;
            if (TowerManager.CanUpgrade(_towerId))
                _upgradeButton.Activate();
            else
                _upgradeButton.Deactivate();
        }

        private void OnEnable()
        {
            EventManager.OnTowerClicked += OnTowerClicked;
            EventManager.OnCurrencyChanged += OnCurrencyChanged;
        }

        private void OnDisable()
        {
            EventManager.OnTowerClicked -= OnTowerClicked;
            EventManager.OnCurrencyChanged -= OnCurrencyChanged;

            // ensure range hidden if controller disabled
            if (_currentTower != null)
                _currentTower.SetRangeVisible(false);
        }
    }
}
