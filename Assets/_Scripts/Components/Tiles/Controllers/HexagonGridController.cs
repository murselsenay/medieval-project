using Cysharp.Threading.Tasks;
using DG.Tweening;
using Modules.ObjectPoolSystem;
using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;
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

            var center = await ObjectPool.GetObjectAsync<HexagonController>(_hexagonHolder, "hexagon");
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

                        var ctrl = await ObjectPool.GetObjectAsync<HexagonController>(_hexagonHolder, "hexagon");
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

            var center = await ObjectPool.GetObjectAsync<HexagonController>(_hexagonHolder, "hexagon");
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
                        var ctrl = await ObjectPool.GetObjectAsync<HexagonController>(_hexagonHolder, "hexagon");
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
    }
}