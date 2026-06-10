using System.Collections;
using System.Collections.Generic;
using Modules.EventSystem.Managers;
using UnityEngine;
namespace Modules.Game.Character.Controllers
{
    public class CharacterController : MonoBehaviour
    {
        [BHeader("References")]
        [SerializeField] private CharacterAnimationController _animationController;
        [SerializeField] private CharacterMovementController _movementController;

        public void ReportMovementSpeed(float speed)
        {
            if (_animationController != null)
                _animationController.SetMovementSpeed(speed);
        }

        public void OnDestinationSet(Vector3 dest)
        {
            
        }

        public void OnStartedRunning()
        {
            if (_animationController != null)
                _animationController.SetRunning(true);
        }

        public void OnStoppedRunning()
        {
            if (_animationController != null)
                _animationController.SetRunning(false);
        }

        private void Awake()
        {
            if (_movementController == null) _movementController = GetComponent<CharacterMovementController>();
            if (_animationController == null) _animationController = GetComponent<CharacterAnimationController>();
            EventManager.OnCharacterSetSpeed += HandleCharacterSetSpeed;
            EventManager.OnCharacterRunStarted += HandleCharacterRunStarted;
            EventManager.OnCharacterRunStopped += HandleCharacterRunStopped;
            EventManager.OnCharacterDestinationSet += HandleCharacterDestinationSet;
        }

        private void OnDestroy()
        {
            EventManager.OnCharacterSetSpeed -= HandleCharacterSetSpeed;
            EventManager.OnCharacterRunStarted -= HandleCharacterRunStarted;
            EventManager.OnCharacterRunStopped -= HandleCharacterRunStopped;
            EventManager.OnCharacterDestinationSet -= HandleCharacterDestinationSet;
        }

        private void HandleCharacterSetSpeed(Transform character, float speed)
        {
            if (character != transform) return;
            if (_animationController != null) _animationController.SetMovementSpeed(speed);
        }

        private void HandleCharacterRunStarted(Transform character)
        {
            if (character != transform) return;
            if (_animationController != null) _animationController.SetRunning(true);
        }

        private void HandleCharacterRunStopped(Transform character)
        {
            if (character != transform) return;
            if (_animationController != null) _animationController.SetRunning(false);
        }

        private void HandleCharacterDestinationSet(Transform character, Vector3 dest)
        {
            if (character != transform) return;
            OnDestinationSet(dest);
        }
    }
}
