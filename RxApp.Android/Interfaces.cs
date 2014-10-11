using ReactiveUI;
using System;
using System.ComponentModel;

namespace RxApp
{
    public interface IRxApplication : IService, IAndroidApplication
    {
        INavigationStack NavigationStack { get; }

        void OnActivityCreated(IRxActivity activity);
    }

    public interface IRxActivity : IActivity, IViewFor, INotifyPropertyChanged
    {
    }

    public interface IRxActivity<TViewModel> : IRxActivity, IViewFor<TViewModel>, INotifyPropertyChanged
        where TViewModel: class
    {
    }
}