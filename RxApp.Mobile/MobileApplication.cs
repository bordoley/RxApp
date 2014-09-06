using System;

namespace RxApp
{
    public static class MobileApplication
    {
        public static IDisposableService Bind(this INavigationStackViewModel<IMobileModel> navStack, Func<INavigationViewController> controllerProvider)
        {
            // FIXMe: PReconditions/Code contracts
            return new MobileApplicationImpl(navStack, controllerProvider);
        }

        private sealed class MobileApplicationImpl : IDisposableService
        {
            private readonly INavigationStackViewModel<IMobileModel> navStack;
            private readonly Func<INavigationViewController> controllerProvider;

            private IDisposable navStackBinding = null;

            internal MobileApplicationImpl(INavigationStackViewModel<IMobileModel> navStack, Func<INavigationViewController> controllerProvider)
            {
                this.navStack = navStack;
                this.controllerProvider = controllerProvider;
            }

            public void Start()
            {
                if (navStackBinding != null)
                {
                    throw new NotSupportedException("Calling start more than once in a row without first calling stop");
                }

                navStackBinding = navStack.Bind(controllerProvider());
            }

            public void Stop()
            {
                navStackBinding.Dispose();
                navStackBinding = null;
            }

            public void Dispose()
            {
                if (navStackBinding != null)
                {
                    navStackBinding.Dispose();
                    navStackBinding = null;
                }
            }
        }
    }
}