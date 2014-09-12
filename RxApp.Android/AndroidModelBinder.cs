using Android.Content;
using System;
using System.Diagnostics.Contracts;
using System.Reactive.Disposables;

namespace RxApp
{
    public static class AndroidModelBinder
    {
        public static IModelBinder<IMobileModel> Create(
            Context context,
            IModelBinder<IMobileControllerModel> controllerBinder, 
            Func<IMobileViewModel, Type> viewTypeMap)
        {
            Contract.Requires(context != null);
            Contract.Requires(controllerBinder != null);
            Contract.Requires(viewTypeMap != null);

            return new AndroidModelBinderImpl(controllerBinder, context, viewTypeMap);
        }

        private static void PresentView(this Context context, Type viewType)
        {
            var intent = new Intent(context, viewType).SetFlags(ActivityFlags.NewTask);
            context.StartActivity(intent);
        }

        private sealed class AndroidModelBinderImpl : IModelBinder<IMobileModel>
        {
            private readonly IModelBinder<IMobileControllerModel> controllerBinder;
            private readonly Context context;
            private readonly Func<IMobileViewModel, Type> viewTypeMap;

            internal AndroidModelBinderImpl(
                IModelBinder<IMobileControllerModel> controllerBinder, 
                Context context,
                Func<IMobileViewModel, Type> viewTypeMap)
            {
                this.controllerBinder = controllerBinder;
                this.context = context;
                this.viewTypeMap = viewTypeMap;
            }

            public void Initialize()
            {
                controllerBinder.Initialize();
            }
                
            public void Dispose()
            {
                controllerBinder.Dispose();
            }

            public IDisposable Bind(IMobileModel model)
            {
                Contract.Requires(model != null);

                context.PresentView(viewTypeMap(model));

                var retval = new CompositeDisposable();
                retval.Add(controllerBinder.Bind(model));
                retval.Add(new AndroidViewBinding(model));
                return retval;
            }
        }

        private sealed class AndroidViewBinding : IDisposable
        {
            private readonly IMobileControllerModel model;

            internal AndroidViewBinding(IMobileControllerModel model)
            {
                this.model = model;
            }

            public void Dispose()
            {
                model.Close.Execute(null);
            }
        }
    } 
}