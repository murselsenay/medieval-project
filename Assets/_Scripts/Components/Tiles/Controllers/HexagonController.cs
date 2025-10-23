using Components.Tiles.Enums;
using Cysharp.Threading.Tasks;
using Modules.ObjectPoolSystem;
using NaughtyAttributes;
using Scriptables.Singletons;
using System.Collections.Generic;
using UnityEngine;
using Components.Minions.Controllers;
using Modules.EventSystem.Managers; // added for event delegation
using UnityEngine.EventSystems;
using Modules.GameState.Managers; // for IsPointerOverGameObject check

namespace Components.Tiles.Controllers
{
    public class HexagonController : BaseObject
    {
        [SerializeField] private MeshRenderer _renderer;
        [SerializeField] private EHexagonState _state;

        [SerializeField] private List<MinionController> _minionsOnHex = new List<MinionController>();
        public IReadOnlyList<MinionController> MinionsOnHex => _minionsOnHex.AsReadOnly();

        // Static reference to currently selected hexagon (only one allowed at a time)
        private static HexagonController _currentlySelected;
        public static HexagonController CurrentlySelected => _currentlySelected;

        // store previous state so we can restore when deselecting
        private EHexagonState _previousState;

        public EHexagonState State
        {
            get { return _state; }
            set
            {
                _state = value;

                switch (_state)
                {
                    case EHexagonState.Enabled:
                        ActivateHexagon();
                        break;
                    case EHexagonState.Disabled:
                        DeactivateHexagon();
                        break;
                    case EHexagonState.Spawn:
                        SpawnHexagon();
                        break;
                    case EHexagonState.Selected:
                        Selected();
                        break;
                }
            }
        }

        // Called when this hexagon becomes selected (internal behavior)
        private void Selected()
        {
            Debug.Log($"Hexagon.Selected() called on {name}", this);

            // set currently selected early
            _currentlySelected = this;

            var mat0 = ResourceWarehouse.Instance?.HexagonSelectedGreenMaterial;
            var mat1 = ResourceWarehouse.Instance?.HexagonBrownMaterial;

            if (_renderer != null && mat0 != null && mat1 != null)
            {
                _renderer.sharedMaterials = new Material[] { mat0, mat1 };
            }
            else
            {
                Debug.LogWarning($"Hexagon.Selected: renderer or materials missing on {name}", this);
            }

            _state = EHexagonState.Selected;

            // notify listeners
            try
            {
                EventManager.DelegateHexagonSelected(this);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error delegating hexagon selected: {ex}", this);
            }
        }

        // Deselect and restore previous state
        public void Deselect()
        {
            Debug.Log($"Hexagon.Deselect() called on {name}", this);

            // If this hex isn't currently selected, nothing to do
            if (_currentlySelected != this) return;

            // Restore previous appearance/state
            switch (_previousState)
            {
                case EHexagonState.Enabled:
                    ActivateHexagon();
                    break;
                case EHexagonState.Spawn:
                    SpawnHexagon();
                    break;
                case EHexagonState.Disabled:
                    DeactivateHexagon();
                    break;
                default:
                    // fallback to enabled
                    ActivateHexagon();
                    break;
            }

            _currentlySelected = null;

            // notify listeners that this hex was deselected
            try
            {
                EventManager.DelegateHexagonDeselected(this);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error delegating hexagon deselected: {ex}", this);
            }
        }

        // Public entry used by grid raycast-based click handling to ensure consistent behavior
        public void HandleClickFromGrid()
        {
            // If clicking a non-enabled hexagon (Disabled or Spawn), clear current selection
            if (_state != EHexagonState.Enabled)
            {
                if (_currentlySelected != null)
                {
                    _currentlySelected.Deselect();
                }
                return;
            }

            // Now it's enabled: toggle selection
            if (_currentlySelected == this)
            {
                Deselect();
                return;
            }

            if (_currentlySelected != null)
            {
                _currentlySelected.Deselect();
            }

            _previousState = _state;
            _currentlySelected = this;
            Selected();
        }

        private void OnMouseDown()
        {
            if (GameStateManager.CurrentState != Modules.GameState.Enums.EGameState.Build) return;
            // ignore clicks when pointer is over UI
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            Debug.Log($"Hexagon clicked: state={_state}, currentlySelected={(_currentlySelected==null?"null":_currentlySelected.name)}", this);
            // Delegate to same handler so both direct OnMouseDown and grid raycast use same logic
            HandleClickFromGrid();
        }

        public async void BombedHexagon(int damage)
        {
            // Cache current state so we can restore visuals properly after the red flash
            var originalState = _state;

            var mat0 = ResourceWarehouse.Instance?.HexagonRedMaterial;
            var mat1 = ResourceWarehouse.Instance?.HexagonRedMaterial;

            if (_renderer != null && mat0 != null && mat1 != null)
                _renderer.sharedMaterials = new Material[] { mat0, mat1 };

            if (_minionsOnHex.Count > 0)
            {
                var temp = new List<MinionController>(_minionsOnHex);
                for (int i = 0; i < temp.Count; i++)
                {
                    var m = temp[i];
                    if (m == null || !m.gameObject.activeInHierarchy || m.IsDead) continue;
                    m.TakeDamage(damage);
                }
            }

            await UniTask.Delay(500);

            // Restore visuals to match state at impact time
            ApplyVisualForState(originalState);
        }

        private void ApplyVisualForState(EHexagonState state)
        {
            if (_renderer == null || ResourceWarehouse.Instance == null) return;

            Material mat0 = null;
            Material mat1 = null;

            switch (state)
            {
                case EHexagonState.Enabled:
                    mat0 = ResourceWarehouse.Instance.HexagonGreenMaterial;
                    mat1 = ResourceWarehouse.Instance.HexagonBrownMaterial;
                    break;
                case EHexagonState.Spawn:
                    mat0 = ResourceWarehouse.Instance.HexagonLightGreenMaterial;
                    mat1 = ResourceWarehouse.Instance.HexagonLightGreenMaterial;
                    break;
                case EHexagonState.Disabled:
                    mat0 = ResourceWarehouse.Instance.HexagonGrayMaterial;
                    mat1 = ResourceWarehouse.Instance.HexagonGrayMaterial;
                    break;
                case EHexagonState.Selected:
                    mat0 = ResourceWarehouse.Instance.HexagonSelectedGreenMaterial;
                    mat1 = ResourceWarehouse.Instance.HexagonBrownMaterial;
                    break;
                default:
                    mat0 = ResourceWarehouse.Instance.HexagonGreenMaterial;
                    mat1 = ResourceWarehouse.Instance.HexagonBrownMaterial;
                    break;
            }

            if (mat0 != null && mat1 != null)
            {
                _renderer.sharedMaterials = new Material[] { mat0, mat1 };
            }
        }

        public void AddMinion(MinionController minion)
        {
            if (minion == null) return;
            if (!_minionsOnHex.Contains(minion)) _minionsOnHex.Add(minion);
        }

        public void RemoveMinion(MinionController minion)
        {
            if (minion == null) return;
            _minionsOnHex.Remove(minion);
        }

        // Remove stale entries: null, inactive, dead, or no longer on this hex
        public void PruneMinionList()
        {
            if (_minionsOnHex.Count == 0) return;
            for (int i = _minionsOnHex.Count - 1; i >= 0; i--)
            {
                var m = _minionsOnHex[i];
                if (m == null || !m.gameObject.activeInHierarchy || m.IsDead)
                {
                    _minionsOnHex.RemoveAt(i);
                    continue;
                }
                // If minion's current hex reference exists and differs, remove
                var currentHexField = m.GetType().GetField("_currentHex", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                HexagonController currentHex = null;
                if (currentHexField != null)
                {
                    currentHex = currentHexField.GetValue(m) as HexagonController;
                }
                if (currentHex != null && currentHex != this)
                {
                    _minionsOnHex.RemoveAt(i);
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            var minion = other.GetComponentInParent<MinionController>();
            if (minion != null)
            {
                AddMinion(minion);
            }
        }

        private void OnTriggerStay(Collider other)
        {
            // Handles cases where minions are spawned already inside the trigger (e.g., Spawn hex)
            var minion = other.GetComponentInParent<MinionController>();
            if (minion != null)
            {
                AddMinion(minion);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            var minion = other.GetComponentInParent<MinionController>();
            if (minion != null)
            {
                RemoveMinion(minion);
            }
        }

        public void SpawnHexagon()
        {
            var lightGreen = ResourceWarehouse.Instance?.HexagonLightGreenMaterial;

            if (_renderer != null && lightGreen != null)
                _renderer.sharedMaterials = new Material[] { lightGreen, lightGreen };

            _state = EHexagonState.Spawn;
        }
        public void ActivateHexagon()
        {
            var mat0 = ResourceWarehouse.Instance?.HexagonGreenMaterial;
            var mat1 = ResourceWarehouse.Instance?.HexagonBrownMaterial;

            if (_renderer != null && mat0 != null && mat1 != null)
                _renderer.sharedMaterials = new Material[] { mat0, mat1 };

            _state = EHexagonState.Enabled;
        }

        public void DeactivateHexagon()
        {
            var gray = ResourceWarehouse.Instance?.HexagonGrayMaterial;

            if (_renderer != null && gray != null)
                _renderer.sharedMaterials = new Material[] { gray, gray };

            _state = EHexagonState.Disabled;
        }
    }
}