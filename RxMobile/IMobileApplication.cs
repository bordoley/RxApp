using System;

namespace RxMobile
{
    public interface IMobileApplication : IDisposable
    {
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
            return new MobileApplicationImpl(viewStackBinder);
        }
       
        internal sealed class MobileApplicationImpl : IMobileApplication
        {
            private readonly IController viewStackBinder;

            internal MobileApplicationImpl(IController viewStackBinder)
            {
                this.viewStackBinder = viewStackBinder;
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