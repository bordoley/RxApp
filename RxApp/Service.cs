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

            CompositeDisposable subscription = new CompositeDisposable();

            IDisposable serv = null;

            subscription.Add (model.Start.Subscribe(_ =>  
                {
                    if (serv == null)
                    {
                        model.CanStart = false;
                        serv = start ();
                    }
                }));

            subscription.Add (model.Stop.Subscribe(_ => 
                {
                    if (serv != null)
                    {
                        serv.Dispose();
                    }

                    model.CanStart = true;
                    serv = null;
                }));

            return subscription;
        }
    }
}

