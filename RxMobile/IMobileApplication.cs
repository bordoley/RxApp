using System;

namespace RxMobile
{
    public interface IMobileApplication : IService
    {
        void PresentView(INavigableViewModel model);
        IService ProvideController(INavigableControllerModel model);
    }
}