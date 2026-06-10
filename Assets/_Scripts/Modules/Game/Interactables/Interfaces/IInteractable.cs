using Game.Interactables.Controllers;
using Modules.EventSystem.Managers;
using Modules.Logger;

namespace Game.Interactables.Interfaces
{
    public interface IInteractable
    {
        void Interact();
        void InteractStarted();
        void InteractEnded();
    }
}