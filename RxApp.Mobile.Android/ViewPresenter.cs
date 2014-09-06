using System;
using Android.Content;

namespace RxApp
{
    public static class AndroidViewPresenter
    {
        public static IViewPresenter Create(Func<object, Type> provideViewType, Context context)
        {
            // FIXME: Contracts/ PReconditions
            return new AndroidViewPresenterImpl(provideViewType, context);
        }

        private sealed class AndroidViewPresenterImpl : IViewPresenter
        {
            private readonly Func<object, Type> provideViewType;
            private readonly Context context;

            internal AndroidViewPresenterImpl(Func<object, Type> provideViewType, Context context)
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
    } 
}