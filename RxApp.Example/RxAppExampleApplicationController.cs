using RxApp;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;

using RxDisposable = System.Reactive.Disposables.Disposable;
using RxObservable = System.Reactive.Linq.Observable;

namespace RxApp.Example
{
    public static class RxAppExampleApplicationController
    {
        public static IConnectableObservable<IEnumerable<INavigationModel>> Create()
        {
            var builder = new NavigationApplicationBuilder();
            builder.RootState = RxObservable.Return(new MainModel());
            builder.RegisterControllerProvider<IMainControllerModel>(model =>
                MainControllerService.Create(model));
            return builder.Build();
        }
    }
}