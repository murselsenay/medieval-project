using Cysharp.Threading.Tasks;
using DG.Tweening;
using Modules.ObjectPoolSystem;
using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Components.Tiles.Enums;

namespace Components.Tiles.Controllers
{
    public class HexagonGridController : MonoBehaviour
    {
        [SerializeField] private Transform _hexagonHolder;
        [SerializeField]
        private Vector3[] _positions = new Vector3[]
        {
        new Vector3(2,0,0),
        new Vector3(1,0,1.75f),
        new Vector3(-1,0,1.75f),
        new Vector3(-2,0,0),
        new Vector3(-1,0,-1.75f),
        new Vector3(1,0,-1.75f),
        };

        [SerializeField] private int _spawnCount = 20;

        [SerializeField] private int _spawnDelayMs = 50;

        [SerializeField, Min(0)] private int _enabledRings = 1;

        private readonly List<HexagonController> _spawned = new List<HexagonController>();
        public List<HexagonController> Hexagons { get { return _spawned; } }

        private void Start()
        {
            SpawnGridButton();
        }

        private void Update()
        {
            // Only react on left mouse down and when not clicking UI
            if (!UnityEngine.Input.GetMouseButtonDown(0)) return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            // Raycast to see if a hexagon was clicked
            Ray ray = Camera.main != null ? Camera.main.ScreenPointToRay(UnityEngine.Input.mousePosition) : new Ray();
            if (Physics.Raycast(ray, out var hit, 100f))
            {
                var hex = hit.collider.GetComponentInParent<HexagonController>();
                if (hex != null)
                {
                    // if clicked hex is not enabled (e.g. disabled or spawn) and it's not the currently selected one, clear current selection
                    if (hex.State != EHexagonState.Enabled && hex != HexagonController.CurrentlySelected)
                    {
                        if (HexagonController.CurrentlySelected != null)
                        {
                            HexagonController.CurrentlySelected.Deselect();
                        }
                    }

                    // clicked a hexagon -> do nothing further (hex's own OnMouseDown will handle enabled selection)
                    return;
                }
            }

            // Do not deselect when clicking empty space (to avoid tower colliders blocking clicks).
            // Selection will only be cleared when user clicks a disabled/spawn hexagon.
        }

        [Button]
        public async void Instantiate()
        {
            await ObjectPool.InitAsync();
            await SpawnHexGridAsync(_spawnCount);
        }

        [Button]
        public void DeactivateAll()
        {
            for (int i = 0; i < _spawned.Count; i++)
            {
                var h = _spawned[i];
                if (h != null)
                {
                    ObjectPool.ReturnToPool(h);
                }
            }
            _spawned.Clear();
        }

        public async UniTask SpawnHexGridAsync(int count)
        {
            if (count <= 0) return;
            if (_positions == null || _positions.Length < 6)
            {
                Debug.LogError("_positions must contain 6 neighbor offsets for hex directions.");
                return;
            }

            int placed = 0;

            var center = await ObjectPool.GetObjectAsync<HexagonController>(_hexagonHolder);
            if (center != null)
            {
                center.transform.localPosition = Vector3.zero;
                PlaySpawnTween(center.transform);

                if (0 <= _enabledRings) center.ActivateHexagon();
                else center.DeactivateHexagon();

                _spawned.Add(center);
                placed++;
                await UniTask.Delay(_spawnDelayMs);
            }

            if (placed >= count) return;

            int radius = 1;
            Vector3[] dirs = _positions;

            while (placed < count)
            {
                Vector3 pos = dirs[4] * radius;

                for (int side = 0; side < 6; side++)
                {
                    int steps = radius;

                    for (int step = 0; step < steps; step++)
                    {
                        if (placed >= count) break;

                        var ctrl = await ObjectPool.GetObjectAsync<HexagonController>(_hexagonHolder);
                        if (ctrl != null)
                        {
                            ctrl.transform.localPosition = pos;
                            PlaySpawnTween(ctrl.transform);

                            if (radius <= _enabledRings)
                                ctrl.ActivateHexagon();
                            else if (radius == _enabledRings + 1)
                                ctrl.SpawnHexagon();
                            else
                                ctrl.DeactivateHexagon();

                            _spawned.Add(ctrl);
                            placed++;
                            await UniTask.Delay(_spawnDelayMs);
                        }

                        pos += dirs[side];
                    }

                    if (placed >= count) break;
                }

                radius++;
            }
        }

        public async UniTask SpawnHexGridByRingsAsync(int rings)
        {
            if (rings < 0) return;
            if (_positions == null || _positions.Length < 6)
            {
                Debug.LogError("_positions must contain 6 neighbor offsets for hex directions.");
                return;
            }

            await ObjectPool.InitAsync();

            var center = await ObjectPool.GetObjectAsync<HexagonController>(_hexagonHolder);
            if (center != null)
            {
                center.transform.localPosition = Vector3.zero;
                PlaySpawnTween(center.transform);
                if (0 <= _enabledRings) center.ActivateHexagon(); else center.DeactivateHexagon();
                _spawned.Add(center);
                await UniTask.Delay(_spawnDelayMs);
            }

            Vector3[] dirs = _positions;

            for (int radius = 1; radius <= rings; radius++)
            {
                Vector3 pos = dirs[4] * radius;

                for (int side = 0; side < 6; side++)
                {
                    int steps = radius;
                    for (int step = 0; step < steps; step++)
                    {
                        var ctrl = await ObjectPool.GetObjectAsync<HexagonController>(_hexagonHolder);
                        if (ctrl != null)
                        {
                            ctrl.transform.localPosition = pos;
                            PlaySpawnTween(ctrl.transform);

                            if (radius <= _enabledRings)
                                ctrl.ActivateHexagon();
                            else if (radius == _enabledRings + 1)
                                ctrl.SpawnHexagon();
                            else
                                ctrl.DeactivateHexagon();

                            _spawned.Add(ctrl);
                            await UniTask.Delay(_spawnDelayMs);
                        }

                        pos += dirs[side];
                    }
                }
            }
        }

        [Button("Spawn Grid")]
        public async void SpawnGridButton()
        {
            await ObjectPool.InitAsync();
            await SpawnHexGridByRingsAsync(_spawnCount);
        }

        private void PlaySpawnTween(Transform t)
        {
            if (t == null) return;

            Vector3 finalPos = t.localPosition;
            t.localPosition = finalPos + Vector3.up * 2f;
            t.localScale = Vector3.one * 0.5f;

            Sequence seq = DOTween.Sequence();
            seq.Append(t.DOLocalMove(finalPos, 0.35f).SetEase(Ease.OutBack));
            seq.Join(t.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack));
        }

        public List<HexagonController> GetNeighborsByIndex(int index, int count = 3)
        {
            var result = new List<HexagonController>();
            if (index < 0 || index >= _spawned.Count) return result;
            if (_positions == null || _positions.Length < 6) return result;

            var center = _spawned[index];
            if (center == null) return result;

            Vector3 centerLocal = GetLocalPosition(center.transform);

            for (int i = 0; i < _positions.Length && result.Count < count; i++)
            {
                Vector3 neighborLocal = centerLocal + _positions[i];
                var neighbor = FindHexagonByLocalPosition(neighborLocal);
                if (neighbor != null)
                {
                    result.Add(neighbor);
                }
            }

            return result;
        }

        public List<HexagonController> GetNeighborsByTransform(Transform hexTransform, int count = 3)
        {
            var result = new List<HexagonController>();
            if (hexTransform == null) return result;

            int idx = _spawned.FindIndex(h => h != null && h.transform == hexTransform);
            if (idx >= 0)
            {
                return GetNeighborsByIndex(idx, count);
            }

            Vector3 localPos = GetLocalPosition(hexTransform);

            var center = FindHexagonByLocalPosition(localPos);
            if (center == null) return result;

            int centerIdx = _spawned.IndexOf(center);
            if (centerIdx < 0) return result;

            return GetNeighborsByIndex(centerIdx, count);
        }

        private Vector3 GetLocalPosition(Transform t)
        {
            if (_hexagonHolder == null || t == null) return Vector3.zero;
            if (t.parent == _hexagonHolder) return t.localPosition;
            // convert world position to holder local
            return _hexagonHolder.InverseTransformPoint(t.position);
        }

        private HexagonController FindHexagonByLocalPosition(Vector3 localPos)
        {
            const float threshold = 0.25f;
            for (int i = 0; i < _spawned.Count; i++)
            {
                var h = _spawned[i];
                if (h == null) continue;
                Vector3 hLocal = GetLocalPosition(h.transform);
                if (Vector3.Distance(hLocal, localPos) <= threshold)
                    return h;
            }
            return null;
        }
    }
}