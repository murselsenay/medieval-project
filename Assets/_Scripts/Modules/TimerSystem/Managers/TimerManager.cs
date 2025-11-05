using System.Threading;
using Cysharp.Threading.Tasks;
using System;
using Utilities.Extensions;
using Modules.EventSystem.Managers;

namespace Modules.TimerSystem.Managers
{
    public static class TimerManager
    {
        private static long _currentTimeUnix;
        public static long CurrentTime
        {
            get => Interlocked.Read(ref _currentTimeUnix);
            set => Interlocked.Exchange(ref _currentTimeUnix, value);
        }

        private static CancellationTokenSource _cts;
        private static bool _initialized = false;

        public static void Init()
        {
            if (_initialized) return;
            _initialized = true;

            // Initialize current time from UTC now as unix timestamp
            CurrentTime = DateTime.UtcNow.ToUnixTime();

            // Start the ticking loop
            Start();
        }

        public static void Start()
        {
            if (_cts != null && !_cts.IsCancellationRequested) return;
            _cts = new CancellationTokenSource();
            RunLoopAsync(_cts.Token).Forget();
        }

        public static void Stop()
        {
            if (_cts == null) return;
            try { _cts.Cancel(); } catch { }
            _cts = null;
        }

        public static void SetCurrentTime(long unixTime)
        {
            CurrentTime = unixTime;
        }

        public static DateTime GetCurrentDateTime()
        {
            return DateTimeExtensions.FromUnixTime(CurrentTime);
        }

        private static async UniTaskVoid RunLoopAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    await UniTask.Delay(1000, cancellationToken: token);
                    Interlocked.Increment(ref _currentTimeUnix);

                    // Trigger global timer tick event
                    EventManager.DelegateTimerTick(CurrentTime);
                }
            }
            catch (OperationCanceledException)
            {
                // expected when stopping
            }
        }
    }
}