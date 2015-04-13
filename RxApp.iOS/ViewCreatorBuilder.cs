using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;

using Foundation;
using UIKit;

using RxObservable = System.Reactive.Linq.Observable;
using System.Reflection;
using System.Reactive.Subjects;

namespace RxApp.iOS
{
    public sealed class ViewCreatorBuilder
    {
        private static UIViewController CreateViewController (
            IReadOnlyDictionary<Type, Func<INavigationViewModel,UIViewController>> modelToViewCreator,
            INavigationViewModel model)
        {
            var modelType = model.GetType();

            foreach (var iface in Enumerable.Concat(new Type[] { modelType }, modelType.GetTypeInfo().ImplementedInterfaces))
            {
                Func<INavigationViewModel,UIViewController> viewCreator;
                if (modelToViewCreator.TryGetValue(iface, out viewCreator))
                {
                    return viewCreator(model);
                }
            }

            throw new NotSupportedException("No UIViewController found for the given model type: " + modelType);
        }

        private readonly Dictionary<Type, Func<INavigationViewModel,UIViewController>> modelToViewCreator =
            new Dictionary<Type, Func<INavigationViewModel,UIViewController>>();

        public void RegisterViewCreator<TModel, TView>(Func<TModel,TView> viewCreator)
            where TModel : INavigationViewModel
            where TView : UIViewController, IReadOnlyViewFor<TModel>
        {
            this.modelToViewCreator.Add(
                typeof(TModel), 
                model => viewCreator((TModel) model));
        }

        public Func<INavigationViewModel,UIViewController> Build()
        {
            var modelToViewCreator = this.modelToViewCreator.ToImmutableDictionary();

            return model => CreateViewController(modelToViewCreator, model);
        }
    }
}

