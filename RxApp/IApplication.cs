using System;

namespace RxApp
{
    public interface IApplication : IDisposable
    {
        void Init();

        IDisposable Bind(object controllerModel);
    }
}

