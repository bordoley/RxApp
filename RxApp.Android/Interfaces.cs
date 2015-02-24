using System;
using System.ComponentModel;

using Android.Views;

namespace RxApp.Android
{
    public interface IRxApplication : IAndroidApplication
    {
        void OnActivityCreated(IRxActivity activity);
    }
        
    public interface IRxActivity : IActivity, IViewFor
    {
    }

    public interface IRxActivity<TViewModel> : IRxActivity, IViewFor<TViewModel>
        where TViewModel: INavigationViewModel
    {
    }
}