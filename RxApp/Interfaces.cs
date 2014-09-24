using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using ReactiveUI;

namespace RxApp
{
    public interface IInitializable : IDisposable
    {
        void Initialize();
    }
        
    public interface IViewHost<TView>
    {
        void PresentView(TView view);
    }
}

