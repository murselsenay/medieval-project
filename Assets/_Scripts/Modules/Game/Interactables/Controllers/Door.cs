using Game.Interactables.Interfaces;
using Modules.EventSystem.Managers;
using Modules.Logger;
using UnityEngine;
namespace Game.Interactables.Controllers
{
    public class Door : MonoBehaviour, IInteractable
    {
        public void Interact()
        {
            //Open Door
            EventManager.DelegateInteracted(this);
        }

        private void OpenDoor() { }
        private void CloseDoor() { }

        public void InteractStarted()
        {
            //
        }

        public void InteractEnded()
        {
            //
        }
    }
}