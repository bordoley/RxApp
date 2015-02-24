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
        private readonly IRxCommand activate;
        private readonly IRxCommand deactivate;

        private readonly IRxProperty<bool> canActivate = RxProperty.Create<bool>(true);

        protected MobileModel()
        {
            activate = this.canActivate.ToCommand();
            deactivate = this.canActivate.Select(x => !x).ToCommand();
        }

        public IRxProperty<bool> CanActivate
        { 
            get { return canActivate; }
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

        IObservable<Unit> IActivationControllerModel.Deactivate
        {
            get
            {
                return deactivate;
            }
        }

        public IRxCommand Deactivate
        {
            get
            {
                return deactivate;
            }
        }

        IObservable<Unit> IActivationControllerModel.Activate
        {
            get
            {
                return activate;
            }
        }

        public IRxCommand Activate
        {
            get
            {
                return activate;
            }
        }  
    }
}