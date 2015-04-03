using System;

using RxDisposable = System.Reactive.Disposables.Disposable;

namespace RxApp
{
    public static class Disposable
    {
        /// <summary>
        /// Returns an IDisposable that disposes a group of Disposables together.
        /// </summary>
        /// <param name="disposables">The disposables that will be disposed together.</param>
        public static IDisposable Compose(params IDisposable[] disposables)
        {
            disposables = disposables ?? new IDisposable[]{};
            return RxDisposable.Create(() => 
                {
                    foreach (var disposable in disposables) { disposable.Dispose(); }
                });
        }
    }
}

