using System;
using System.ComponentModel;

namespace RxApp
{
    public interface IRxApplication : IAndroidApplication
    {
        void OnActivityCreated(IRxActivity activity);
    }

    // FIXME: Consider exposing Activity callbacks as observables
    // obvious example is OnOptionsItemSelected. Its a slippery slope though.
    public interface IRxActivity : IActivity, IViewFor
    {
    }

    public interface IRxActivity<TViewModel> : IRxActivity, IViewFor<TViewModel>
        where TViewModel: class
    {
    }
}