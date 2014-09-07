using System;
using Android.Content;

namespace RxApp
{
    public static class AndroidModelBinder
    {
        public static IModelBinder<IMobileModel> Create(
            Context context,
            IModelBinder<IMobileControllerModel> controllerBinder, 
            Func<IMobileViewModel, Type> viewTypeMap)
        {
            // FIXME: Preconditions/Code contracts
            return new AndroidModelBinderImpl(controllerBinder, context, viewTypeMap);
        }

        private static void PresentView(this Context context, Type viewType)
        {
            var intent = new Intent(context, viewType).SetFlags(ActivityFlags.NewTask | ActivityFlags.SingleTop);
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
                context.PresentView(viewTypeMap(model));
                return controllerBinder.Bind(model);
            }
        }
    } 
}