using UnityEngine;
using Modules.Economy.Enums;
using Modules.PopupSystem.Components;
using System;
using Game.Interactables.Interfaces;

namespace Modules.EventSystem.Managers
{
    public static class EventManager
    {
        #region Events
        public static event Action<Transform, ECurrencyType, int> OnSpawnCurrencyRequest;
        public static event Action<ECurrencyType, int> OnCurrencyChanged;
        public static event Action<ECurrencyType> OnCurrencyRequestCompleted;
        public static event Action<BasePopup> OnPopupShowed;
        public static event Action<BasePopup> OnPopupClosed;

        // Timer
        public static event Action<long> OnTimerTick;
        // Enemy animation / control events
        public static event Action<Transform, float> OnEnemySetSpeed;
        public static event Action<Transform> OnEnemySpotted;
        public static event Action<Transform> OnEnemySpottedCleared;


        // Character
        public static event Action<Vector3, float> OnCharacterMoveInput;
        public static event Action OnCharacterMoveStarted;
        public static event Action OnCharacterMoveEnded;
        public static event Action<Transform, float> OnCharacterSetSpeed;
        public static event Action<Transform> OnCharacterRunStarted;
        public static event Action<Transform> OnCharacterRunStopped;
        public static event Action<Transform, Vector3> OnCharacterDestinationSet;

        // Interaction
        public static event Action<IInteractable> OnInteractionStarted;
        public static event Action<IInteractable> OnInteractionEnded;
        public static event Action<IInteractable> OnInteracted;
        #endregion

        #region Character 
        public static void DelegateCharacterMoveInput(Vector3 direction, float magnitude)
        {
            try
            {
                OnCharacterMoveInput?.Invoke(direction, magnitude);
            }
            catch { }
        }

        public static void DelegateCharacterMoveStarted()
        {
            try
            {
                OnCharacterMoveStarted?.Invoke();
            }
            catch { }
        }

        public static void DelegateCharacterMoveEnded()
        {
            try
            {
                OnCharacterMoveEnded?.Invoke();
            }
            catch { }
        }

        public static void DelegateCharacterSetSpeed(Transform character, float speed)
        {
            try
            {
                OnCharacterSetSpeed?.Invoke(character, speed);
            }
            catch { }
        }

        public static void DelegateCharacterRunStarted(Transform character)
        {
            try
            {
                OnCharacterRunStarted?.Invoke(character);
            }
            catch { }
        }

        public static void DelegateCharacterRunStopped(Transform character)
        {
            try
            {
                OnCharacterRunStopped?.Invoke(character);
            }
            catch { }
        }

        public static void DelegateCharacterDestinationSet(Transform character, Vector3 dest)
        {
            try
            {
                OnCharacterDestinationSet?.Invoke(character, dest);
            }
            catch { }
        }

        #endregion

        #region Enemy
        public static void DelegateEnemySetSpeed(Transform enemy, float normalized)
        {
            try
            {
                OnEnemySetSpeed?.Invoke(enemy, normalized);
            }
            catch { }
        }

        public static void DelegateEnemySpotted(Transform enemy)
        {
            try
            {
                OnEnemySpotted?.Invoke(enemy);
            }
            catch { }
        }

        public static void DelegateEnemySpottedCleared(Transform enemy)
        {
            try
            {
                OnEnemySpottedCleared?.Invoke(enemy);
            }
            catch { }
        }
        #endregion

        #region Timer
        public static void DelegateTimerTick(long unixTime)
        {
            try
            {
                OnTimerTick?.Invoke(unixTime);
            }
            catch { }
        }

        #endregion

        #region Popup
        public static void DelegatePopupShowed(BasePopup popup)
        {
            try
            {
                OnPopupShowed?.Invoke(popup);
            }
            catch { }
        }

        public static void DelegatePopupClosed(BasePopup popup)
        {
            try
            {
                OnPopupClosed?.Invoke(popup);
            }
            catch { }
        }
        #endregion

        #region Currency
        public static void DelegateSpawnCurrencyRequest(Transform spawnPoint, ECurrencyType type, int amount)
        {
            try
            {
                OnSpawnCurrencyRequest?.Invoke(spawnPoint, type, amount);
            }
            catch { }
        }

        public static void DelegateCurrencyChanged(ECurrencyType type, int amount)
        {
            try
            {
                OnCurrencyChanged?.Invoke(type, amount);
            }
            catch { }
        }

        public static void DelegateCurrencyRequestCompleted(ECurrencyType type)
        {
            try
            {
                OnCurrencyRequestCompleted?.Invoke(type);
            }
            catch { }
        }
        #endregion

        #region Interaction
        public static void DelegateInteractionStarted(IInteractable interactable)
        {
            try
            {
                OnInteractionStarted?.Invoke(interactable);
            }
            catch { }
        }
        public static void DelegateInteractionEnded(IInteractable interactable)
        {
            try
            {
                OnInteractionEnded?.Invoke(interactable);
            }
            catch { }
        }
        public static void DelegateInteracted(IInteractable interactable)
        {
            try
            {
                OnInteracted?.Invoke(interactable);
            }
            catch { }
        }

        #endregion

    }
}
