using Components.Towers.Models;
using Modules.TowerSystem.Managers;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using Modules.EventSystem.Managers;

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
        [SerializeField] private Canvas _canvas; // Screen Space - Overlay canvas
        [SerializeField] private RectTransform _panelRect;
        [SerializeField] private Vector3 _worldOffset = new Vector3(0f, 1.5f, 0f);

        // Short debounce window to ignore hides immediately after showing (in seconds)
        private float _ignoreHideUntil = 0f;
        [SerializeField] private float _hideDebounceSeconds = 0.25f;

        private void Awake()
        {
            if (_panelRect == null)
                _panelRect = GetComponentInChildren<RectTransform>();

            // Ensure visuals start hidden
            ToggleContent(false);
        }

        private void OnEnable()
        {
            EventManager.OnTowerClicked += OnTowerClicked;
        }

        private void OnDisable()
        {
            EventManager.OnTowerClicked -= OnTowerClicked;
        }

        private void Update()
        {
            if (!UnityEngine.Input.GetMouseButtonDown(0))
                return;

            // If pointer is over UI, ignore (prevents dismissing when interacting with UI)
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            // If we are within debounce window after showing, ignore hides
            if (Time.realtimeSinceStartup < _ignoreHideUntil)
                return;

            // Also ignore hides if user clicked inside the panel rect (so clicking the UI won't close it)
            if (_panelRect != null && RectTransformUtility.RectangleContainsScreenPoint(_panelRect, UnityEngine.Input.mousePosition, null))
                return;

            // Clicked somewhere that's not UI and not a tower click -> hide
            Hide();
        }

        private void OnTowerClicked(int towerId)
        {
            // Find tower by instance id and show panel
            var towers = GameObject.FindGameObjectsWithTag(Components.Constants.GameObjectTags.Tower);
            if (towers == null || towers.Length == 0)
            {
                Hide();
                return;
            }

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

            // set debounce window so a quick second click doesn't immediately hide the panel
            _ignoreHideUntil = Time.realtimeSinceStartup + _hideDebounceSeconds;
        }

        public void Initialize(BaseTower tower)
        {
            if (tower == null) return;

            int towerId = tower.TowerId;
            var info = TowerManager.GetCurrentLevelInfo(towerId);

            if (_towerRangeText != null)
                _towerRangeText.text = info.Data.ProjectileRange.ToString();

            if (_towerHealthText != null)
                _towerHealthText.text = info.Data.Health.ToString();

            if (_towerDamageText != null)
                _towerDamageText.text = info.DPS.ToString();

            if (_towerNameText != null)
                _towerNameText.text = info.Data.TowerType.ToString();
        }

        public void ShowForTower(BaseTower tower)
        {
            if (tower == null || _panelRect == null || _canvas == null) return;

            Initialize(tower);

            ToggleContent(true);

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
            ToggleContent(false);
        }

        private void ToggleContent(bool active)
        {
            if (_panelRect == null) return;
            _panelRect.gameObject.SetActive(active);
        }
    }
}
