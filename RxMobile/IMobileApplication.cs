using System;

namespace RxMobile
{
    public interface IMobileApplication 
    {
        void Start();
        void Stop();

        void PresentView(IMobileViewModel model);
        IController ProvideController(IMobileControllerModel model);
    }
}