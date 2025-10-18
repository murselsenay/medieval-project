using Modules.ObjectPoolSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Components.Projectiles.Controllers;
using Cysharp.Threading.Tasks;
using Components.Minions.Controllers;

namespace Components.Towers.Controllers
{
    public class MainTower : BaseTower
    {
        protected override async UniTaskVoid FireAtTarget(Transform target)
        {
            if (target == null) return;

            // Validate target (may have died between selection and firing)
            var minion = target.GetComponentInParent<MinionController>();
            if (minion == null || minion.IsDead) return;

            ArrowProjectile proj = null;

            proj = await ObjectPool.GetObjectAsync<ArrowProjectile>(StartTransform ?? transform);

            if (proj == null)
            {
                Debug.LogWarning("MainTowerController: ArrowProjectile not found in pool.");
                return;
            }

            // Re-validate after await in case target died during pooling
            if (minion == null || minion.IsDead) return;

            Vector3 startPos = (StartTransform ?? transform).position;
            Vector3 targetPos = minion.transform.position;
            var targetCollider = minion.GetComponent<Collider>();
            if (targetCollider != null)
            {
                targetPos = targetCollider.ClosestPoint(startPos);
            }

            proj.Launch(StartTransform ?? transform, targetPos, _projectileSpeed, null, null, _projectileDamage, gameObject.tag, gameObject);

            Debug.DrawLine(startPos, targetPos, Color.red, 0.5f);
        }
    }
}