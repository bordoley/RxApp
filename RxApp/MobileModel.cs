using System;
using System.Windows.Input;
using ReactiveUI;

namespace RxApp
{
    public class MobileModel : ReactiveObject, INavigableControllerModel, INavigableViewModel, IServiceControllerModel, IServiceViewModel 
    {
        private readonly IReactiveCommand<object> back = ReactiveCommand.Create();
        private readonly IReactiveCommand<object> up = ReactiveCommand.Create();
        private readonly IReactiveCommand<object> start;
        private readonly IReactiveCommand<object> stop;

        private bool canStart = true;
        private bool canStop = false;

        public MobileModel()
        {
            start = ReactiveCommand.Create(this.WhenAnyValue(x => x.CanStart));
            stop = ReactiveCommand.Create(this.WhenAnyValue(x => x.CanStop));
        }

        public bool CanStart 
        { 
            internal get { return canStart; }
            set { this.RaiseAndSetIfChanged(ref canStart, value); }
        }

        public bool CanStop
        { 
            internal get { return canStop; }
            set { this.RaiseAndSetIfChanged(ref canStop, value); }
        }

        IObservable<object> INavigableControllerModel.Back
        {
            get
            {
                return back;
            }
        }

        public ICommand Back
        {
            get
            {
                return back;
            }
        }

        IObservable<object> INavigableControllerModel.Up
        {
            get
            {
                return up;
            }
        }

        public ICommand Up
        {
            get
            {
                return up;
            }
        }

        IObservable<object> IServiceControllerModel.Stop
        {
            get
            {
                return stop;
            }
        }

        public ICommand Stop
        {
            get
            {
                return stop;
            }
        }

        IObservable<object> IServiceControllerModel.Start
        {
            get
            {
                return start;
            }
        }

        public ICommand Start
        {
            get
            {
                return start;
            }
        }  
    }
}