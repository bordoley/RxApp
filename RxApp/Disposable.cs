using System;
using RxDisposable = System.Reactive.Disposables.Disposable;

namespace RxApp
{
    public static class Disposable
    {
        public static IDisposable Compose(IDisposable first, IDisposable second)
        {
            return RxDisposable.Create(() => 
                {
                    first.Dispose();
                    second.Dispose();
                });
        }

        public static IDisposable Compose(IDisposable first, IDisposable second, params IDisposable[] disposables)
        {
            return RxDisposable.Create(() => 
                {
                    first.Dispose();
                    second.Dispose();
                    foreach (var disposable in disposables) { disposable.Dispose(); }
                });
        }
    }
}

