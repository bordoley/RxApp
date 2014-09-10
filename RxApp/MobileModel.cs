using System;
using ReactiveUI;

namespace RxApp
{
    public interface IMobileViewModel : INavigableViewModel, IServiceViewModel {}
    public interface IMobileControllerModel : INavigableControllerModel, IServiceControllerModel {}
    public interface IMobileModel : IMobileViewModel, IMobileControllerModel, INavigableModel, IServiceModel {}

    public class MobileModel : IMobileModel
    {
        private readonly IReactiveCommand<object> back = ReactiveCommand.Create();
        private readonly IReactiveCommand<object> up = ReactiveCommand.Create();
        private readonly IReactiveCommand<object> close = ReactiveCommand.Create();
        private readonly IReactiveCommand<object> stop = ReactiveCommand.Create();
        private readonly IReactiveCommand<object> start = ReactiveCommand.Create();

        IObservable<object> INavigableControllerModel.Back
        {
            get
            {
                return back;
            }
        }

        public IReactiveCommand<object> Back
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

        public IReactiveCommand<object> Up
        {
            get
            {
                return up;
            }
        }

        IObservable<object> INavigableViewModel.Close
        {
            get
            {
                return close;
            }
        }

        public IReactiveCommand<object> Close
        {
            get
            {
                return close;
            }
        }

        IObservable<object> IServiceControllerModel.Stop
        {
            get
            {
                return stop;
            }
        }

        public IReactiveCommand<object> Stop
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

        public IReactiveCommand<object> Start
        {
            get
            {
                return start;
            }
        }  
    }
}