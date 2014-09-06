using System;

namespace RxApp
{
    public static class MobileApplication
    {
        public static IInitializableService Create(INavigationStack<IMobileModel> navStack, IModelBinder<IMobileModel> binder)
        {
            // FIXMe: PReconditions/Code contracts
            return new MobileApplicationImpl(navStack, binder);
        }

        private sealed class MobileApplicationImpl : IInitializableService
        {
            private readonly INavigationStack<IMobileModel> navStack;
            private readonly IModelBinder<IMobileModel> binder;

            private IDisposable modelBinding = null;

            internal MobileApplicationImpl(INavigationStack<IMobileModel> navStack, IModelBinder<IMobileModel> binder)
            {
                this.navStack = navStack;
                this.binder = binder;
            }

            public void Start()
            {
                if (modelBinding != null)
                {
                    throw new NotSupportedException("Calling start more than once in a row without first calling stop");
                }

                modelBinding = binder.Bind(navStack);
            }

            public void Stop()
            {
                modelBinding.Dispose();
                modelBinding = null;
            }

            public void Initialize()
            {
                binder.Initialize();
            }

            public void Dispose()
            {
                if (modelBinding != null)
                {
                    modelBinding.Dispose();
                    modelBinding = null;
                }

                binder.Dispose();
            }
        }
    }
}