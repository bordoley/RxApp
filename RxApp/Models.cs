using System;
using System.Reactive;
using System.Reactive.Linq;

namespace RxApp
{
    public abstract class ActivationModel : IActivationViewModel, IActivationControllerModel
    {
        private readonly IRxCommand activate;
        private readonly IRxCommand deactivate;
        private readonly IRxProperty<bool> canActivate = RxProperty.Create<bool>(true);

        protected ActivationModel()
        {
            activate = this.canActivate.ToCommand();
            deactivate = this.canActivate.Select(x => !x).ToCommand();
        }

        IObservable<Unit> IActivationControllerModel.Deactivate { get { return deactivate; } }

        IRxCommand IActivationViewModel.Deactivate { get { return deactivate; } }


        IObservable<Unit> IActivationControllerModel.Activate { get { return activate; } }

        IRxCommand IActivationViewModel.Activate { get { return activate; } }  


        IRxProperty<bool> IActivationControllerModel.CanActivate { get { return canActivate; } }
    }

    public abstract class NavigationModel : ActivationModel, INavigationModel
    {
        private readonly IRxCommand back = RxCommand.Create();
        private readonly IRxCommand up = RxCommand.Create();
        private readonly IRxCommand<INavigationModel> open = RxCommand.Create<INavigationModel>();

        protected NavigationModel()
        {
        }

        public IRxCommand Back { get { return back; } }

        IObservable<Unit> INavigationStackControllerModel<INavigationModel>.Back { get { return back; } }


        public IRxCommand Up { get { return up; } }

        IObservable<Unit> INavigationStackControllerModel<INavigationModel>.Up { get { return up; } }


        public IRxCommand<INavigationModel> Open { get { return open; } }

        IObservable<INavigationModel> INavigationStackControllerModel<INavigationModel>.Open { get { return open; } }
    }
}