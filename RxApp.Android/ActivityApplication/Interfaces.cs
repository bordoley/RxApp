using ReactiveUI;
using System;
using System.ComponentModel;

namespace RxApp
{
    public interface IActivityViewApplication : IService, IAndroidApplication
    {
        INavigationStack<IMobileModel> NavigationStack { get; }

        void OnActivityViewCreated(IActivityView activity);
    }

    public interface IActivityView : IActivity, IViewFor, INotifyPropertyChanged
    {
    }

    public interface IActivityView<TViewModel> : IActivityView, IViewFor<TViewModel>
        where TViewModel : class, IMobileViewModel
    {
    }
}