using System.Collections.Generic;
using Game.Interactables.Interfaces;
using Modules.EventSystem.Managers;
using UnityEngine;
using Utilities;

namespace Game.Interactables.Controllers.Base
{
    public class Interactor : MonoBehaviour
    {
        [SerializeField] private GameObject _interactableObject;
        [BHeader("Collider")]
        [SerializeField] private SphereCollider _interactionCollider;
        [Range(0, 10)]
        [SerializeField] private float _interactionColliderRadius;
        [BHeader("Indicator")]
        [SerializeField] private List<string> _interactableTags;

        private IInteractable _interactable;
        public List<string> GetInteractableTags() => _interactableTags;
        public bool IsInteractableWithTag(string tag) => _interactableTags.Contains(tag);
        private void Awake()
        {
            _interactable = _interactableObject.GetComponent<IInteractable>();
        }
        public void OnValidate()
        {
            _interactionCollider.radius = _interactionColliderRadius;
        }

        #region Interaction Detection

        private void OnTriggerEnter(Collider other)
        {
            if (IsInteractableWithTag(other.gameObject.tag))
            {
                _interactable.InteractStarted();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (IsInteractableWithTag(other.gameObject.tag))
            {
                _interactable.InteractEnded();
            }

        }
        #endregion
    }
}