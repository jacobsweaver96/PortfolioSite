using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace PortfolioSite.Utils
{
    public class ThreadManager
    {
        #region Singleton
        private static ThreadManager _current;

        private ThreadManager() { }

        public static ThreadManager Current
        {
            get
            {
                if (_current == null)
                {
                    _current = new ThreadManager();
                }

                return _current;
            }
        }
        #endregion

        public volatile Boolean IsSafeAbort = false;

        private List<ThreadWrapper> ActiveThreads { get; } = new List<ThreadWrapper>();

        public void AddThread(ThreadWrapper thread)
        {
            ActiveThreads.Add(thread);
        }

        public async Task SafeAbort(int threadId)
        {
            IsSafeAbort = true;

            var thread = ActiveThreads.SingleOrDefault(v => v.Thread.ManagedThreadId == threadId);

            if (!thread.Thread.IsAlive)
            {
                ActiveThreads.Remove(thread);
                return;
            }

            thread.CancellationToken.Cancel();

            // Give it a chance to cancel itself
            await Task.Delay(100);

            if (thread.Thread.IsAlive)
            {
                // Kill it
                thread.Thread.Abort();
            }

            ActiveThreads.Remove(thread);
        }

        public async Task SafeAbortAll()
        {
            var threads = ActiveThreads.Where(v => v.Thread.IsAlive);

            foreach (var t in threads)
            {
                await SafeAbort(t.Thread.ManagedThreadId);
            }

            ActiveThreads.Clear();
        }
    }

    public class ThreadWrapper
    {
        public ThreadWrapper(Thread thread, CancelToken cancellationToken)
        {
            Thread = thread;
            CancellationToken = cancellationToken;
        }

        public Thread Thread { get; private set; }

        public volatile CancelToken CancellationToken;
    }

    public class CancelToken
    {
        private bool _isCancelled;
        public bool IsCancelled { get => _isCancelled; }

        public void Cancel()
        {
            _isCancelled = true;
        }
    }
}