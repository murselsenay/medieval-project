using Modules.ObjectPoolSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Components.Projectiles.Controllers;
using Cysharp.Threading.Tasks;
using Components.Tiles.Controllers;
using Components.Tiles.Enums;
using Components.Minions.Controllers;

namespace Components.Towers.Controllers
{
    public class AreaTower : BaseTower
    {
        private float _localCooldownTimer = 0f;

        [Header("Debug")]
        [SerializeField] private bool _debugLogs = false;
        private void DLog(string msg)
        {
            if (_debugLogs)
                Debug.Log($"[AreaTower:{name} t={Time.time:F2}] {msg}");
        }

        private void Awake()
        {
            // disable base auto-targeting; AreaTower uses hex-based firing
            _useAutoTargeting = false;
        }

        private void Update()
        {
            if (!gameObject.activeInHierarchy) return;

            if (_localCooldownTimer > 0f) _localCooldownTimer -= Time.deltaTime;
            if (_localCooldownTimer > 0f)
            {
                DLog($"On cooldown {_localCooldownTimer:F2}s remaining");
                return;
            }

            var grid = Object.FindObjectOfType<HexagonGridController>();
            if (grid == null || grid.Hexagons == null || grid.Hexagons.Count == 0)
            {
                DLog("Grid or Hexagons list is null/empty. Skipping.");
                return; // nothing to evaluate
            }

            Vector3 towerPos = transform.position;
            float rangeSq = _range * _range;

            // Find the hexagon the tower is currently standing on
            HexagonController towerHex = null;
            float minDistSq = float.MaxValue;
            for (int i = 0; i < grid.Hexagons.Count; i++)
            {
                var hex = grid.Hexagons[i];
                if (hex == null) continue;
                float sqrDist = (hex.transform.position - towerPos).sqrMagnitude;
                if (sqrDist < minDistSq)
                {
                    minDistSq = sqrDist;
                    towerHex = hex;
                }
            }
            // Use a reasonable threshold to ensure the tower is actually on this hex
            float hexMatchThreshold = 0.1f; // adjust if needed
            float hexMatchThresholdSq = hexMatchThreshold * hexMatchThreshold;

            HexagonController chosen = null;
            int bestCount = 0;
            float bestHexDistSq = float.MaxValue;

            // diagnostics
            int totalAliveSeen = 0;
            int totalCountedInRange = 0;
            int totalSkippedInactive = 0;
            int totalSkippedDead = 0;
            int totalSkippedWrongHex = 0;
            int totalSkippedOutOfRange = 0;

            for (int i = 0; i < grid.Hexagons.Count; i++)
            {
                var hex = grid.Hexagons[i];
                if (hex == null) continue;
                if (hex.State != EHexagonState.Enabled && hex.State != EHexagonState.Spawn) continue;

                // SKIP: Do not target the hexagon the tower is placed on
                if (towerHex != null && (hex.transform.position - towerPos).sqrMagnitude < hexMatchThresholdSq)
                    continue;

                int count = 0;
                var minions = hex.MinionsOnHex;
                DLog($"Hex '{hex.name}' state={hex.State} listedMinions={minions.Count}");
                for (int m = 0; m < minions.Count; m++)
                {
                    var mn = minions[m];
                    if (mn == null) { continue; }
                    if (!mn.gameObject.activeInHierarchy) { totalSkippedInactive++; continue; }
                    if (mn.IsDead) { totalSkippedDead++; continue; }
                    totalAliveSeen++;
                    if (mn.CurrentHex != null && mn.CurrentHex != hex) { totalSkippedWrongHex++; continue; }
                    var md = mn.transform.position - towerPos; md.y = 0f;
                    var dsq = md.sqrMagnitude;
                    bool inRange = dsq <= rangeSq;
                    if (inRange) { count++; totalCountedInRange++; }
                    else { totalSkippedOutOfRange++; }
                    DLog($"  - mn='{mn.name}' inRange={inRange} distSq={dsq:F2} rangeSq={rangeSq:F2} currentHex={(mn.CurrentHex==null?"null":mn.CurrentHex.name)}");
                }

                if (count <= 0) continue;

                var centerDiff = hex.transform.position - towerPos; centerDiff.y = 0f;
                float centerDistSq = centerDiff.sqrMagnitude;

                if (count > bestCount || (count == bestCount && centerDistSq < bestHexDistSq))
                {
                    bestCount = count;
                    bestHexDistSq = centerDistSq;
                    chosen = hex;
                }
            }

            if (chosen == null)
            {
                DLog($"No target chosen. aliveSeen={totalAliveSeen}, inRange={totalCountedInRange}, skipped: inactive={totalSkippedInactive}, dead={totalSkippedDead}, wrongHex={totalSkippedWrongHex}, outOfRange={totalSkippedOutOfRange}");
                return;
            }

            DLog($"Chosen hex '{chosen.name}' with bestCount={bestCount}");
            FireAtTarget(chosen.transform).Forget();
            _localCooldownTimer = _fireCooldown;
            DLog($"Fired. Cooldown set to {_fireCooldown:F2}");
        }

        // Implement FireAtTarget to launch an AreaProjectile at a chosen hex transform
        protected override async UniTaskVoid FireAtTarget(Transform target)
        {
            if (target == null) { DLog("FireAtTarget called with null target"); return; }

            // Prevent bombing the hexagon the tower is standing on
            var grid = Object.FindObjectOfType<HexagonGridController>();
            if (grid != null && grid.Hexagons != null && grid.Hexagons.Count > 0)
            {
                Vector3 towerPos = transform.position;
                float minDistSq = float.MaxValue;
                HexagonController towerHex = null;
                for (int i = 0; i < grid.Hexagons.Count; i++)
                {
                    var hex = grid.Hexagons[i];
                    if (hex == null) continue;
                    float sqrDist = (hex.transform.position - towerPos).sqrMagnitude;
                    if (sqrDist < minDistSq)
                    {
                        minDistSq = sqrDist;
                        towerHex = hex;
                    }
                }
                float hexMatchThreshold = 0.1f;
                float hexMatchThresholdSq = hexMatchThreshold * hexMatchThreshold;
                if (towerHex != null && (target.position - towerHex.transform.position).sqrMagnitude < hexMatchThresholdSq)
                {
                    DLog($"Prevented firing at own hex '{towerHex.name}'.");
                    // Do not fire at own hex
                    return;
                }
            }

            AreaProjectile proj = null;
            proj = await ObjectPool.GetObjectAsync<AreaProjectile>(StartTransform ?? transform);

            if (proj == null)
            {
                Debug.LogWarning("AreaTower: AreaProjectile not found in pool.");
                DLog("Projectile fetch from pool failed (null).");
                return;
            }

            Vector3 startPos = (StartTransform ?? transform).position;
            Vector3 targetPos = target.position;
            var targetCollider = target.GetComponent<Collider>();
            if (targetCollider != null) targetPos = targetCollider.ClosestPoint(startPos);

            float arcHeight = 2f;
            DLog($"Launching projectile -> speed={_projectileSpeed:F2}, damage={_projectileDamage:F2}, arc={arcHeight:F2}, start={startPos}, target={targetPos}");
            proj.Launch(StartTransform ?? transform, target, _projectileSpeed, null, arcHeight, _projectileDamage, gameObject.tag, gameObject);

            Debug.DrawLine(startPos, targetPos, Color.magenta, 0.5f);
        }
    }
}
