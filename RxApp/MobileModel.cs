using System;
using System.Windows.Input;
using System.Reactive;
using System.Reactive.Linq;

namespace RxApp
{
    public class MobileModel : INavigationModel, IServiceControllerModel, IServiceViewModel 
    {
        private readonly IRxCommand back = RxCommand.Create();
        private readonly IRxCommand up = RxCommand.Create();
        private readonly IRxCommand<INavigationModel> open = RxCommand.Create<INavigationModel>();
        private readonly IRxCommand start;
        private readonly IRxCommand stop;

        private readonly IRxProperty<bool> canStart = RxProperty.Create<bool>(true);

        public MobileModel()
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

        IObservable<Unit> IServiceControllerModel.Stop
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

        IObservable<Unit> IServiceControllerModel.Start
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