using ReactiveUI;
using System;
using System.ComponentModel;

namespace RxApp
{
    public interface IRxApplication : IAndroidApplication
    {
        INavigationStack NavigationStack { get; }

        void OnActivityCreated(IRxActivity activity);

        void Start();

        void Stop();
    }

    public interface IRxActivity : IActivity, IViewFor, INotifyPropertyChanged
    {
    }

    public interface IRxActivity<TViewModel> : IRxActivity, IViewFor<TViewModel>
        where TViewModel: class
    {
    }
}