using System;
using System.Diagnostics.Contracts;
using System.Reactive.Disposables;

namespace RxApp
{
    public static class Service
    {
        public static IDisposable Bind(this IServiceControllerModel model, Func<IDisposable> start)
        {
            Contract.Requires(model != null);
            Contract.Requires(start != null);

            IDisposable serv = null;

            return Disposable.Compose(
                model.Start.Subscribe(_ =>  
                    {
                        if (serv == null)
                        {
                            model.CanStart.Value = false;
                            serv = start ();
                        }
                    }),

                model.Stop.Subscribe(_ => 
                    {
                        if (serv != null)
                        {
                            serv.Dispose();
                        }

                        model.CanStart.Value = true;
                        serv = null;
                    })
            );
        }
    }
}

