using System;
using System.Windows.Input;
using System.Reactive;
using System.Reactive.Linq;

namespace RxApp
{
    public class MobileModel : INavigableControllerModel, INavigableViewModel, IServiceControllerModel, IServiceViewModel 
    {
        private readonly IRxCommand back = RxCommand.Create();
        private readonly IRxCommand up = RxCommand.Create();
        private readonly IRxCommand start;
        private readonly IRxCommand stop;

        private IRxProperty<bool> canStart = RxProperty.Create<bool>(true);

        public MobileModel()
        {
            start = this.canStart.ToCommand();
            stop = this.canStart.Select(x => !x).ToCommand();
        }

        public bool CanStart 
        { 
            internal get { return canStart.Value; }
            set { this.canStart.Value = value; }
        }

        IObservable<Unit> INavigableControllerModel.Back
        {
            get
            {
                return back;
            }
        }

        public IRxCommand Back
        {
            get
            {
                return back;
            }
        }

        IObservable<Unit> INavigableControllerModel.Up
        {
            get
            {
                return up;
            }
        }

        public IRxCommand Up
        {
            get
            {
                return up;
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