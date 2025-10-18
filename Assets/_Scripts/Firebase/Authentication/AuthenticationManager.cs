using System;
using Firebase;
using Firebase.Auth;
using Cysharp.Threading.Tasks;
using Modules.Logger;

namespace Firebase.Authentication
{
    public static class AuthenticationManager
    {
        private static FirebaseAuth Auth => FirebaseAuth.DefaultInstance;

        public static async UniTask<T> AuthenticateAsync<T>(Func<FirebaseAuth, UniTask<T>> authAction)
        {
            if (authAction == null) throw new ArgumentNullException(nameof(authAction));

            var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync().AsUniTask();
            if (dependencyStatus != DependencyStatus.Available)
                DebugLogger.LogError($"Could not resolve all Firebase dependencies: {dependencyStatus}");

            if (FirebaseApp.DefaultInstance == null)
                DebugLogger.LogError("FirebaseApp is not initialized.");

            return await authAction(Auth);
        }

        public static async UniTask<FirebaseUser> SignInAnonymouslyAsync()
        {
            var authResult = await AuthenticateAsync(auth => auth.SignInAnonymouslyAsync().AsUniTask());
            return authResult?.User ?? Auth?.CurrentUser;
        }

        public static void SignOut()
        {
            Auth?.SignOut();
        }

        public static FirebaseUser CurrentUser => Auth?.CurrentUser;
    }
}