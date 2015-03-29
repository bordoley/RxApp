using System;

using RxDisposable = System.Reactive.Disposables.Disposable;

namespace RxApp
{
    public static class Disposable
    {
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

