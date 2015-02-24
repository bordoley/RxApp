using System;
using System.Reactive;
using System.Reactive.Linq;

namespace RxApp
{
    public abstract class NavigationModel : INavigationModel
    {
        private readonly IRxCommand back = RxCommand.Create();
        private readonly IRxCommand up = RxCommand.Create();
        private readonly IRxCommand<INavigationModel> open = RxCommand.Create<INavigationModel>();
        private readonly IRxCommand activate;
        private readonly IRxCommand deactivate;

        private readonly IRxProperty<bool> canActivate = RxProperty.Create<bool>(true);

        protected NavigationModel()
        {
            activate = this.canActivate.ToCommand();
            deactivate = this.canActivate.Select(x => !x).ToCommand();
        }

        public IRxCommand Back { get { return back; } }

        IObservable<Unit> INavigationStackControllerModel<INavigationModel>.Back { get { return back; } }


        public IRxCommand Up { get { return up; } }

        IObservable<Unit> INavigationStackControllerModel<INavigationModel>.Up { get { return up; } }


        public IRxCommand<INavigationModel> Open { get { return open; } }

        IObservable<INavigationModel> INavigationStackControllerModel<INavigationModel>.Open { get { return open; } }


        IObservable<Unit> IActivationControllerModel.Deactivate { get { return deactivate; } }

        IRxCommand IActivationViewModel.Deactivate { get { return deactivate; } }


        IObservable<Unit> IActivationControllerModel.Activate { get { return activate; } }

        IRxCommand IActivationViewModel.Activate { get { return activate; } }  


        IRxProperty<bool> IActivationControllerModel.CanActivate { get { return canActivate; } }

    }
}