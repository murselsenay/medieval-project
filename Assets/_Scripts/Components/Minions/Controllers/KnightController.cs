using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Components.Minions.Enums;

namespace Components.Minions.Controllers
{
    public class KnightController : MinionController
    {
        protected override void Awake()
        {
            base.Awake();
            _minionType = EMinionType.Knight;
            if (_animator == null)
                _animator = GetComponent<Animator>();
        }
    }
}