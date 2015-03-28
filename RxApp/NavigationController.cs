using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RxApp
{
    public sealed class NavigationControllerBuilder
    {
        private readonly Dictionary<Type, Func<INavigationControllerModel,IDisposable>> typeToControllerProvider = 
            new Dictionary<Type, Func<INavigationControllerModel,IDisposable>>();

        public NavigationControllerBuilder()
        {
        }

        public IObservable<INavigationModel> RootState { get; set; }

        public void RegisterControllerProvider<TModel> (Func<TModel, IDisposable> controllerProvider)
            where TModel : INavigationControllerModel
        {
            this.typeToControllerProvider.Add(typeof(TModel), model => 
                controllerProvider((TModel) model));
        }

        public INavigationController Build()
        {
            return new NavigationController(
                this.RootState, 
                this.typeToControllerProvider.ToDictionary(
                    entry => entry.Key,
                    entry => entry.Value));
        }

        private sealed class NavigationController : INavigationController
        {
            private readonly IObservable<INavigationModel> rootState;
            private readonly IReadOnlyDictionary<Type, Func<INavigationControllerModel,IDisposable>> typeToControllerProvider = 
                new Dictionary<Type, Func<INavigationControllerModel,IDisposable>>();

            internal NavigationController(
                IObservable<INavigationModel> rootState,
                IReadOnlyDictionary<Type, Func<INavigationControllerModel,IDisposable>> typeToControllerProvider)
            {
                this.rootState = rootState;
                this.typeToControllerProvider = typeToControllerProvider;
            }

            public IObservable<INavigationModel> RootState { get { return rootState; } }

            public IDisposable Bind(INavigationControllerModel model)
            {
                var modelType = model.GetType();
                foreach (var iface in Enumerable.Concat(new Type[] { modelType }, modelType.GetTypeInfo().ImplementedInterfaces))
                {
                    Func<INavigationControllerModel,IDisposable> controllerProvider;
                    if (this.typeToControllerProvider.TryGetValue(iface, out controllerProvider))
                    {
                        return controllerProvider(model);
                    }
                }

                throw new NotSupportedException("No Controller found for the given model type: " + modelType);
            }
        }
    }
}

