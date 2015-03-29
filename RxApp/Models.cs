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

            // Leaking these subscriptions is ok since they will only exist for the lifetime of the object.
            activate.Subscribe(_ => canActivate.Value = false);
            deactivate.Subscribe(_ => canActivate.Value = true);
        }

        IObservable<Unit> IActivationControllerModel.Deactivate { get { return deactivate; } }

        IRxCommand IActivationViewModel.Deactivate { get { return deactivate; } }


        IObservable<Unit> IActivationControllerModel.Activate { get { return activate; } }

        IRxCommand IActivationViewModel.Activate { get { return activate; } }  
    }

    public abstract class NavigationModel : ActivationModel, INavigationModel
    {
        private readonly IRxCommand back;
        private readonly IRxCommand up;
        private readonly IRxCommand<INavigationModel> open;

        protected NavigationModel()
        {
            // Prevent calling back/up/open if the model is not activated
            var activated = (this as IActivationViewModel).Deactivate.CanExecute;

            this.back = activated.ToCommand();
            this.up = activated.ToCommand();
            this.open = activated.ToCommand<INavigationModel>();
        }

        IRxCommand INavigationViewModel.Back { get { return back; } }

        IRxCommand INavigationControllerModel.Back { get { return back; } }

        public IObservable<Unit> Back { get { return back; } }


        IRxCommand INavigationViewModel.Up { get { return up; } }

        IRxCommand INavigationControllerModel.Up { get { return up; } }

        public IObservable<Unit> Up { get { return up; } }


        IRxCommand<INavigationModel> INavigationControllerModel.Open { get { return open; } }

        public IObservable<INavigationModel> Open { get { return open; } }
    }
}