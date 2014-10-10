using System;
using ReactiveUI;

namespace RxApp
{
    internal static class ReactiveObject
    {
        internal class InstantiableReactiveObject : ReactiveUI.ReactiveObject 
        {
        }

        internal static IReactiveObject Create() 
        {
            return new InstantiableReactiveObject();
        }
    }
}

