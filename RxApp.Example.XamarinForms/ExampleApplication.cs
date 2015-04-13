using System;
using Xamarin.Forms;

using RxApp;
using RxApp.XamarinForms;

using System.Reactive.Linq;

namespace RxApp.Example.XamarinForms
{
    public static class ExampleApplication
    {
        public static IDisposable Create(RxFormsApplication app)
        {
            var pageCreatorBuilder = new PageCreatorBuilder();
            pageCreatorBuilder.RegisterPageCreator<IMainViewModel,ExamplePage>(model => new ExamplePage(model));

            return RxAppExampleApplicationController.Create().BindTo(app, pageCreatorBuilder.Build());
        }
    }

    // FIXME: Provide base classes for ContentPage, etc.
    public sealed class ExamplePage : ContentPage, IReadOnlyViewFor<IMainViewModel>
    {
        private readonly IMainViewModel viewModel;
        private readonly RxPageHelper<ExamplePage,IMainViewModel> helper;

        public ExamplePage(IMainViewModel viewModel)
        {
            this.viewModel = viewModel;
            this.helper = RxPageHelper<ExamplePage,IMainViewModel>.Create(this);

            var button = new Button();
            button.Text = "Open new window";
            this.Content = button;

            IDisposable subscription = null;

            this.Appearing += (o,e) =>
                {
                    var x = this.ToolbarItems;
                    subscription = this.viewModel.OpenPage.Bind(button);
                };

            this.Disappearing += (o,e) =>
                {
                    subscription.Dispose();
                    subscription = null;
                };
        }

        public IMainViewModel ViewModel
        {
            get { return this.viewModel; }
        }

        object IReadOnlyViewFor.ViewModel
        {
            get { return this.ViewModel; }
        }

        protected override bool OnBackButtonPressed()
        {
            return this.helper.OnBackButtonPressed();
        }
    }
}

