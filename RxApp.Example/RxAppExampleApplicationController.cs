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
        public static IObservable<IEnumerable<INavigationModel>> Create()
        {
            var builder = new NavigationApplicationBuilder();
            builder.RootState = RxObservable.Return(new MainModel());
            builder.RegisterControllerProvider<IMainControllerModel>(model =>
                MainControllerService.Create(model));
            return builder.Build();
        }
    }
}

