using System;
using System.Diagnostics.Contracts;

namespace RxApp
{
    public sealed class UIViewControllerDelegate<TModel>
        where TModel: class, /*INavigableViewModel,*/ IServiceViewModel
    {
        public static UIViewControllerDelegate<TModel> Create(TModel model)
        {
            Contract.Requires(model != null);
            return new UIViewControllerDelegate<TModel>(model);
        }

        private readonly TModel model;

        private UIViewControllerDelegate(TModel model)
        {
            this.model = model;
        }

        public void ViewDidAppear(bool animated)
        {
            model.Start.Execute(null);
        }

        public void ViewDidDisappear(bool animated)
        {
            model.Stop.Execute(null);
        }
    }
}

