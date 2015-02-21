using RxApp;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;

using RxDisposable = System.Reactive.Disposables.Disposable;
using RxObservable = System.Reactive.Linq.Observable;

namespace RxApp.Example
{
    public class RxAppExampleApplicationController : IApplication
    {
        public RxAppExampleApplicationController()
        {
        }

        public IObservable<INavigationModel> ResetApplicationState
        { 
            get { return RxObservable.Return(new MainModel()); }
        }
            
        public IDisposable Bind(object model)
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

                return (model as IServiceControllerModel).Bind(service);
            }
            else
            {
                return RxDisposable.Empty;
            }
        }
    }
}

