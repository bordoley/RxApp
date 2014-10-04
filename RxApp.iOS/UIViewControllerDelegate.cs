using System;
using System.Diagnostics.Contracts;

namespace RxApp
{
    public sealed class UIViewControllerDelegate
    {
        public static UIViewControllerDelegate Create(IMobileModel model)
        {
            Contract.Requires(model != null);
            return new UIViewControllerDelegate(model);
        }

        private readonly IMobileModel model;

        private UIViewControllerDelegate(IMobileModel model)
        {
            this.model = model;
        }

        public void ViewDidAppear(bool animated)
        {
            ((IMobileViewModel)model).Start.Execute(null);
        }

        public void ViewDidDisappear(bool animated)
        {
            ((IMobileViewModel)model).Stop.Execute(null);
        }
    }
}

