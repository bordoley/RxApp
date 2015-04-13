using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Android.App;
using System.Collections.Immutable;
using Android.Content;

namespace RxApp
{
    public sealed class ActivityCreatorBuilder
    {
        private static Type GetActivityType(
            IReadOnlyDictionary<Type, Type> activityMapping,
            INavigationViewModel model)
        {
            var modelType = model.GetType();

            foreach (var iface in Enumerable.Concat(new Type[] { modelType }, modelType.GetTypeInfo().ImplementedInterfaces))
            {
                Type activityType;
                if (activityMapping.TryGetValue(iface, out activityType))
                {
                    return activityType;
                }
            }

            throw new NotSupportedException("No activity found for the given model type: " + modelType);
        }

        private readonly Dictionary<Type, Type> activityMapping = new Dictionary<Type, Type>();
        private Action<Activity, Type> startActivity;

        public Action<Activity, Type> StartActivity { set { this.startActivity = value; } }

        public void RegisterActivityMapping<TModel, TActivity>()
            where TModel : INavigationViewModel
            where TActivity : Activity, IViewFor<TModel>
        {
            this.activityMapping.Add(typeof(TModel), typeof(TActivity));
        }

        public Action<Activity,INavigationViewModel> Build()
        {
            var activityMapping = this.activityMapping.ToImmutableDictionary();
            var startActivity = this.startActivity ?? ((previous, type) =>
                {
                    var intent = new Intent(previous, type);
                    previous.StartActivity(intent);
                });

            return (previous, model) =>
                {
                    var viewType = GetActivityType(activityMapping, model);
                    startActivity(previous, viewType);
                };
        }
    }
}

