using System;

namespace RxMobile
{
    public interface IMobileApplication : IDisposable
    {
        // FIXME: LeakyAbstraction
        IViewStack<IMobileModel> ViewStack { get; }

        void Run();
    }

    public static class MobileApplication
    {
        public static IMobileApplication Create(
            IViewStack<IMobileModel> viewStack, 
            IViewPresenter viewPresenter, 
            IControllerProvider controllerProvider)
        {
            var viewStackBinder = ViewStackBinder<IMobileModel>.Create(viewStack, viewPresenter, controllerProvider);
            return new MobileApplicationImpl(viewStack, viewStackBinder);
        }
       
        internal sealed class MobileApplicationImpl : IMobileApplication
        {
            private readonly IViewStack<IMobileModel> viewStack;
            private readonly IController viewStackBinder;

            internal MobileApplicationImpl(IViewStack<IMobileModel> viewStack, IController viewStackBinder)
            {
                this.viewStack = viewStack;
                this.viewStackBinder = viewStackBinder;
            }

            public IViewStack<IMobileModel> ViewStack
            {
                get
                {
                    return viewStack;
                }
            }

            public void Run()
            {
                viewStackBinder.Initialize();
            }

            public void Dispose()
            {
                viewStackBinder.Dispose();
            }
        }
    }
}