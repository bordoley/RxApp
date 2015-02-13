using System;
using System.Windows.Input;
using ReactiveUI;
using System.Reactive;
using System.Reactive.Linq;

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

        IObservable<Unit> INavigableControllerModel.Back
        {
            get
            {
                return back.Select(_ => Unit.Default);
            }
        }

        public ICommand Back
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
                return up.Select(_ => Unit.Default);
            }
        }

        public ICommand Up
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
                return stop.Select(_ => Unit.Default);
            }
        }

        public ICommand Stop
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
                return start.Select(_ => Unit.Default);
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