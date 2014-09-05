using System;

namespace RxMobile
{
    // Maybe ISupportInitialize or rename IComponent?
    public interface IController : IDisposable
    {
        void Initialize();
    }

    public static class Controller
    {
        public static IController Default() 
        {
            return new DefaultController();
        }

        internal sealed class DefaultController : IController
        {
            public void Initialize()
            {
            }

            public void Dispose()
            {
            }
        }
    }
}