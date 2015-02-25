using System;
using System.Diagnostics.Contracts;
using System.Reactive.Disposables;

namespace RxApp
{
    public static class Activation
    {
        // FIXME: I think we're abusing the naming convention BindTo here
        public static IDisposable BindTo(this IActivationControllerModel model, Func<IDisposable> provideController)
        {
            Contract.Requires(model != null);
            Contract.Requires(provideController != null);

            IDisposable controller = null;

            return Disposable.Compose(
                model.Activate.Subscribe(_ =>  
                    {
                        if (controller == null)
                        {
                            model.CanActivate.Value = false;
                            controller = provideController ();
                        }
                    }),

                model.Deactivate.Subscribe(_ => 
                    {
                        if (controller != null)
                        {
                            controller.Dispose();
                        }

                        model.CanActivate.Value = true;
                        controller = null;
                    })
            );
        }
    }
}

