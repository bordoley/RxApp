using System;
using Xamarin.Forms;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Collections.Immutable;

namespace RxApp.XamarinForms
{
    public sealed class PageCreatorBuilder
    {
        private static Page CreatePage(
            IReadOnlyDictionary<Type, Func<INavigationViewModel,Page>> modelToPageCreator,
            INavigationViewModel model)
        {
            var modelType = model.GetType();

            foreach (var iface in Enumerable.Concat(new Type[] { modelType }, modelType.GetTypeInfo().ImplementedInterfaces))
            {
                Func<INavigationViewModel,Page> pageCreator;
                if (modelToPageCreator.TryGetValue(iface, out pageCreator))
                {
                    return pageCreator(model);
                }
            }

            throw new NotSupportedException("No Page creator found for the given model type: " + modelType);
        }

        private readonly Dictionary<Type, Func<INavigationViewModel,Page>> modelToPageCreator = 
            new Dictionary<Type, Func<INavigationViewModel,Page>>();

        public void RegisterPageCreator<TModel, TView>(Func<TModel,TView> viewCreator)
            where TModel : INavigationViewModel
            where TView : Page, IReadOnlyViewFor<TModel>
        {
            this.modelToPageCreator.Add(
                typeof(TModel), 
                model => viewCreator((TModel) model));
        }

        public Func<INavigationViewModel, Page> Build()
        {
            var modelToPageCreator = this.modelToPageCreator.ToImmutableDictionary();

            return model => CreatePage(modelToPageCreator, model);
        }
    }
}

