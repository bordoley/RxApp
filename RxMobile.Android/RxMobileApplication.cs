using Android.App;
using Android.Content;
using Android.Runtime;
using System;
using System.Reactive.Linq;
using ReactiveUI;

namespace RxMobile
{          
    public sealed class AndroidViewPresenter : IViewPresenter
    {
        public static IViewPresenter Create(Func<object, Type> provideViewType, Context context)
        {
            // FIXME: Contracts/ PReconditions
            return new AndroidViewPresenter(provideViewType, context);
        }

        private readonly Func<object, Type> provideViewType;
        private readonly Context context;

        private AndroidViewPresenter(Func<object, Type> provideViewType, Context context)
        {
            this.provideViewType = provideViewType;
            this.context = context;
        }

        public void PresentView(object viewModel)
        {
            // FIXME: Precondition or Contract checks
            var viewType = provideViewType(viewModel);
            var intent = new Intent(context, viewType).SetFlags(ActivityFlags.NewTask | ActivityFlags.SingleTop);
            context.StartActivity(intent);
        }

        public void Initialize() {}
        public void Dispose() {}
    }

    public abstract class RxMobileApplication : Application
    {
        private IMobileApplication application = null;
        private IActivityLifecycleEvents events = null;
        private int activities = 0;

        public RxMobileApplication(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        protected abstract void SetViewModel(IViewFor view, IMobileViewModel viewModel);
        protected abstract IMobileApplication CreateApplication();

        public override void OnCreate()
        {
            base.OnCreate();

            events = ActivityLifecycleEvents.Register(this);
            events.Created.Subscribe(a => activities++);
            events.Destroyed.Subscribe(_ =>
                {
                    activities--;
                    if (activities <= 0)
                    {
                        application.Dispose();
                        application = null;
                    }
                });

            RxApp.MainThreadScheduler = new WaitForDispatcherScheduler(() => AndroidScheduler.UIScheduler());
        }

        public override void OnTerminate()
        {
            if (application != null)
            {
                application.Dispose();
                application = null;
            }
            base.OnTerminate();
        }

        public void OnViewCreated(IViewFor view)
        {
            if (application.ViewStack.Current != null)
            {
                this.SetViewModel(view, application.ViewStack.Current);
            }
            else
            {
                throw new Exception("RxMobileActivity created when no viewmodel available");
            }
        }

        public void Run()
        {
            application = this.CreateApplication();
            application.Run();
        }
    }
}