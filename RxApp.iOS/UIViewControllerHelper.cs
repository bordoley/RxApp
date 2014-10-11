using System;
using System.Diagnostics.Contracts;

namespace RxApp
{
    public sealed class UIViewControllerHelper<TModel>
        where TModel: class, /*INavigableViewModel,*/ IServiceViewModel
    {
        public static UIViewControllerHelper<TModel> Create(TModel model)
        {
            Contract.Requires(model != null);
            return new UIViewControllerHelper<TModel>(model);
        }

        private readonly TModel model;

        private UIViewControllerHelper(TModel model)
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

