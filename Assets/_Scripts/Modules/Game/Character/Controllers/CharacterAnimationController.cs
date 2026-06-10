using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Modules.Game.Character.Controllers
{
    public class CharacterAnimationController : MonoBehaviour
    {
        [BHeader("Animator")]
        [SerializeField] private Animator _animator;
        [SerializeField] private string _speedParam = "Speed";
        [SerializeField] private string _runBool = "IsRunning";

        public void SetMovementSpeed(float speed)
        {
            if (_animator == null) return;
            _animator.SetFloat(_speedParam, speed);
        }

        public void SetRunning(bool running)
        {
            if (_animator == null) return;
            _animator.SetBool(_runBool, running);
        }
    }
}
