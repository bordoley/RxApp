using ReactiveUI;
using System;
using System.ComponentModel;

namespace RxApp
{
    public interface IRxApplication : IAndroidApplication
    {
        void OnActivityCreated(IRxActivity activity);

        Type GetActivityType(object model);

        void Start();

        void Stop();
    }

    public interface IRxActivity : IActivity, IViewFor
    {
    }

    public interface IRxActivity<TViewModel> : IRxActivity, IViewFor<TViewModel>
        where TViewModel: class
    {
    }
}