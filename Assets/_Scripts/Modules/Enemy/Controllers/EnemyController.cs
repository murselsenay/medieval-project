using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Modules.Enemy.Controllers
{
    [DisallowMultipleComponent]
    public class EnemyController : MonoBehaviour
    {
        [SerializeField] private EnemyPatrol _patrol;

        private void Awake()
        {
            if (_patrol == null)
                _patrol = GetComponent<EnemyPatrol>();
        }

        private void Start()
        {
            if (_patrol != null)
                _patrol.StartPatrol();
        }

        public void SetPatrolTargetsParent(Transform parent)
        {
            if (_patrol != null)
                _patrol.SetTargetsParent(parent);
        }
    }
}

