using System;
using Android.Content;

namespace RxApp
{
    public static class AndroidViewPresenter
    {
        public static void PresentView(Type viewType, Context context)
        {
            // FIXME: Precondition or Contract checks
            var intent = new Intent(context, viewType).SetFlags(ActivityFlags.NewTask | ActivityFlags.SingleTop);
            context.StartActivity(intent);
        }
    } 
}