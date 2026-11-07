using System;
using System.Threading;

namespace IdCardManager.UI.Helpers
{
    /// <summary>
    /// 业务进行中锁定整窗交互（返回、首页卡片、充扣按钮、状态栏重连等）。
    /// UiBusy / 模态流程均应 Enter/Exit。
    /// </summary>
    internal static class NavigationLock
    {
        private static int _depth;

        public static event Action Changed;

        public static bool IsLocked => Volatile.Read(ref _depth) > 0;

        public static void Enter()
        {
            Interlocked.Increment(ref _depth);
            Changed?.Invoke();
        }

        public static void Exit()
        {
            if (Interlocked.Decrement(ref _depth) < 0)
            {
                Interlocked.Exchange(ref _depth, 0);
            }

            Changed?.Invoke();
        }

        /// <summary>using 作用域锁定，适合弹窗前后。</summary>
        public static IDisposable Scope() => new ScopeHandle();

        private sealed class ScopeHandle : IDisposable
        {
            private int _disposed;

            public ScopeHandle()
            {
                Enter();
            }

            public void Dispose()
            {
                if (Interlocked.Exchange(ref _disposed, 1) == 0)
                {
                    Exit();
                }
            }
        }
    }
}
