using UnityEngine;
using Modules.PopupSystem.Enums;
using Modules.PopupSystem.Managers;
using UnityEngine.Events;
using Utilities; // ButtonAnimate lives here

namespace Components.General.Buttons
{
    /// <summary>
    /// A button that opens the selected popup when clicked.
    /// Inherits from ButtonAnimate so it keeps the pressed animation behavior.
    /// </summary>
    public class PopupNavigateButton : ButtonAnimate
    {
        [SerializeField]
        private EPopup TargetPopup = EPopup.None;

        private UnityAction _onClick;

        private void Start()
        {
            _onClick = () =>
            {
                if (TargetPopup != EPopup.None)
                {
                    PopupManager.ShowPopup(TargetPopup);
                }
            };

            // If component enabled at start, add listener now
            if (isActiveAndEnabled)
                AddListener(_onClick);
        }

        private void OnEnable()
        {
            // add listener (ButtonAnimate.AddListener)
            AddListener(_onClick);
        }

        private void OnDisable()
        {
            // remove listeners that this component added
            RemoveAllListeners();
        }
    }
}
