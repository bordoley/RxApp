using System;
using System.Reactive;
using System.Reactive.Linq;

namespace RxApp
{
    public abstract class MobileModel : INavigationModel, IActivationControllerModel, IActivationViewModel 
    {
        private readonly IRxCommand back = RxCommand.Create();
        private readonly IRxCommand up = RxCommand.Create();
        private readonly IRxCommand<INavigationModel> open = RxCommand.Create<INavigationModel>();
        private readonly IRxCommand start;
        private readonly IRxCommand stop;

        private readonly IRxProperty<bool> canStart = RxProperty.Create<bool>(true);

        protected MobileModel()
        {
            start = this.canStart.ToCommand();
            stop = this.canStart.Select(x => !x).ToCommand();
        }

        public IRxProperty<bool> CanStart 
        { 
            get { return canStart; }
        }

        public IRxCommand Back
        {
            get
            {
                return back;
            }
        }

        public IRxCommand Up
        {
            get
            {
                return up;
            }
        }

        public IRxCommand<INavigationModel> Open
        {
            get
            {
                return open;
            }
        }

        IObservable<Unit> IActivationControllerModel.Stop
        {
            get
            {
                return stop;
            }
        }

        public IRxCommand Stop
        {
            get
            {
                return stop;
            }
        }

        IObservable<Unit> IActivationControllerModel.Start
        {
            get
            {
                return start;
            }
        }

        public IRxCommand Start
        {
            get
            {
                return start;
            }
        }  
    }
}