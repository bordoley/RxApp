using System;
using System.Diagnostics.Contracts;
using System.Reactive.Disposables;

namespace RxApp
{
    public static class Activation
    {
        // FIXME: I think we're abusing the naming convention BindTo here
        public static IDisposable BindTo(this IActivationControllerModel This, Func<IDisposable> provideController)
        {
            Contract.Requires(This != null);
            Contract.Requires(provideController != null);

            IDisposable controller = null;

            return Disposable.Compose(
                This.Activate.Subscribe(_ =>  
                    {
                        if (controller == null)
                        {
                            This.CanActivate.Value = false;
                            controller = provideController();
                        }
                    }),

                This.Deactivate.Subscribe(_ => 
                    {
                        if (controller != null)
                        {
                            controller.Dispose();
                        }

                        This.CanActivate.Value = true;
                        controller = null;
                    })
            );
        }
    }
}

