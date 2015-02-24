using System;
using System.Reactive;
using System.Reactive.Linq;

namespace RxApp
{
    public interface IMobileViewModel : INavigationViewModel, IActivationViewModel 
    {
    }

    public interface IMobileControllerModel : INavigationControllerModel<IMobileModel>, IActivationControllerModel
    {
    }

    public interface IMobileModel : IMobileViewModel, IMobileControllerModel, INavigableModel<IMobileModel>
    {
    }

    public abstract class MobileModel : IMobileModel
    {
        private readonly IRxCommand back = RxCommand.Create();
        private readonly IRxCommand up = RxCommand.Create();
        private readonly IRxCommand<IMobileModel> open = RxCommand.Create<IMobileModel>();
        private readonly IRxCommand activate;
        private readonly IRxCommand deactivate;

        private readonly IRxProperty<bool> canActivate = RxProperty.Create<bool>(true);

        protected MobileModel()
        {
            activate = this.canActivate.ToCommand();
            deactivate = this.canActivate.Select(x => !x).ToCommand();
        }

        public IRxCommand Back { get { return back; } }

        IObservable<Unit> INavigableModel<IMobileModel>.Back { get { return back; } }


        public IRxCommand Up { get { return up; } }

        IObservable<Unit> INavigableModel<IMobileModel>.Up { get { return up; } }


        public IRxCommand<IMobileModel> Open { get { return open; } }

        IObservable<IMobileModel> INavigableModel<IMobileModel>.Open { get { return open; } }


        IObservable<Unit> IActivationControllerModel.Deactivate { get { return deactivate; } }

        IRxCommand IActivationViewModel.Deactivate { get { return deactivate; } }


        IObservable<Unit> IActivationControllerModel.Activate { get { return activate; } }

        IRxCommand IActivationViewModel.Activate { get { return activate; } }  


        IRxProperty<bool> IActivationControllerModel.CanActivate { get { return canActivate; } }

    }
}