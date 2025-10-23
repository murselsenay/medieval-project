using System;
using Modules.GameState.Enums;

namespace Modules.GameState.Managers
{
    public static class GameStateManager
    {
        private static EGameState _currentState = EGameState.Start;

        public static EGameState CurrentState => _currentState;

        public static event Action<EGameState> OnStateChanged;

        static GameStateManager()
        {
            // ensure default state is Start on first access
            _currentState = EGameState.Start;
        }

        public static void SetState(EGameState newState)
        {
            if (_currentState == newState) return;
            _currentState = newState;
            try
            {
                OnStateChanged?.Invoke(newState);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"GameStateManager: error during state change invoke: {ex}");
            }
        }
    }
}
