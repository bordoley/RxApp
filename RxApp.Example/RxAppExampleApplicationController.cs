using RxApp;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;

using RxDisposable = System.Reactive.Disposables.Disposable;
using RxObservable = System.Reactive.Linq.Observable;

namespace RxApp.Example
{
    public static class RxAppExampleApplicationController 
    {
        public static IObservable<INavigationModel> RootState
        { 
            get { return RxObservable.Return(new MainModel()); }
        }
            
        public static IDisposable Bind(INavigationControllerModel model)
        {
            // This is a lot prettier if you use F# pattern matching
            if (model is IMainControllerModel)
            {

                Func<IDisposable> service = () =>
                {
                    var ret = new MainControllerService((IMainControllerModel)model);
                    ret.Init();
                    return ret;
                };

                return (model as IActivationControllerModel).Bind(service);
            }
            else
            {
                return RxDisposable.Empty;
            }
        }
    }
}

