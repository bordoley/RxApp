using System;
using ReactiveUI;

namespace RxMobile
{
    public static class ReactiveObjectFactory
    {
        internal class InstantiableReactiveObject : ReactiveObject 
        {
        }

        public static IReactiveObject Create() 
        {
            return new InstantiableReactiveObject();
        }
    }
}

