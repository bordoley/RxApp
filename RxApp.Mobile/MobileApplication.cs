using System;

namespace RxApp
{
    public static class MobileApplication
    {
        public static IInitableService Create(IModelStack<IMobileModel> modelStack, IModelBinder<IMobileModel> binder)
        {
            // FIXMe: PReconditions/Code contracts
            return new MobileApplicationImpl(modelStack, binder);
        }

        private sealed class MobileApplicationImpl : IInitableService
        {
            private readonly IModelStack<IMobileModel> modelStack;
            private readonly IModelBinder<IMobileModel> binder;

            private IDisposable modelBinding = null;

            internal MobileApplicationImpl(IModelStack<IMobileModel> modelStack, IModelBinder<IMobileModel> binder)
            {
                this.modelStack = modelStack;
                this.binder = binder;
            }

            public void Start()
            {
                if (modelBinding != null)
                {
                    throw new NotSupportedException("Calling start more than once in a row without first calling stop");
                }

                modelBinding = binder.Bind(modelStack);
            }

            public void Stop()
            {
                modelBinding.Dispose();
                modelBinding = null;
            }

            public void Init()
            {
                binder.Init();
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