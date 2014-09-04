using System;
using ReactiveUI;

namespace RxMobile
{
    public class MobileModel : ReactiveObject, INavigableModel, ILifecycleModel
    {
        private readonly IReactiveCommand<object> back = ReactiveCommand.Create();
        private readonly IReactiveCommand<object> up = ReactiveCommand.Create();
        private readonly IReactiveCommand<object> close = ReactiveCommand.Create();
        private readonly IReactiveCommand<object> pausing = ReactiveCommand.Create();
        private readonly IReactiveCommand<object> resuming = ReactiveCommand.Create();

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

        IObservable<object> ILifecycleControllerModel.Pausing
        {
            get
            {
                return pausing;
            }
        }

        public IReactiveCommand<object> Pausing
        {
            get
            {
                return pausing;
            }
        }

        IObservable<object> ILifecycleControllerModel.Resuming
        {
            get
            {
                return resuming;
            }
        }

        public IReactiveCommand<object> Resuming
        {
            get
            {
                return resuming;
            }
        }  
    }
}