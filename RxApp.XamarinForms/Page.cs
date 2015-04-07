using System;
using RxApp;
using Xamarin.Forms;

namespace RxApp.XamarinForms
{
    public sealed class RxPageHelper<TPage, TViewModel>
        where TPage : Page, IReadOnlyViewFor<TViewModel>
        where TViewModel: INavigationViewModel
    {
        public static RxPageHelper<TPage,TViewModel> Create(TPage page)
        {
            return new RxPageHelper<TPage,TViewModel>(page);
        }

        private readonly TPage page;

        private RxPageHelper(TPage page)
        {
            this.page = page;
            page.Appearing += (o,e) =>
                {
                    page.ViewModel.Activate.Execute();
                };

            page.Disappearing += (o,e) =>
                {
                    page.ViewModel.Deactivate.Execute();
                };
        }

        public bool OnBackButtonPressed()
        {
            page.ViewModel.Back.Execute();
            return true;
        }
    }
}

