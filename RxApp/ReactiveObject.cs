using System;
using ReactiveUI;

namespace RxApp
{
    public static class ReactiveObject
    {
        internal class InstantiableReactiveObject : ReactiveUI.ReactiveObject 
        {
        }

        public static IReactiveObject Create() 
        {
            return new InstantiableReactiveObject();
        }
    }
}

