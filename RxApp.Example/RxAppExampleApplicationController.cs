using RxApp;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reactive.Linq;
using System.Reactive.Subjects;

using RxDisposable = System.Reactive.Disposables.Disposable;
using RxObservable = System.Reactive.Linq.Observable;
using System.Linq;

namespace RxApp.Example
{
    public static class RxAppExampleApplicationController
    {
        public static IConnectableObservable<ImmutableStack<INavigationModel>> Create()
        {
            var builder = new NavigationApplicationBuilder();
            builder.RootState = RxObservable.Return(ImmutableStack.Create<INavigationModel>(new MainModel()));
            builder.RegisterBinding<IMainControllerModel>(model => MainControllerService.Create(model));
            return builder.Build();
        }
    }
}