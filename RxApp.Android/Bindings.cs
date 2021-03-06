﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;

using Android.Text;
using Android.Views;
using Android.Widget;
using System.Linq.Expressions;

using RxObservable = System.Reactive.Linq.Observable;
using RxDisposable = System.Reactive.Disposables.Disposable;
using AndroidBuild = Android.OS.Build;

using System.Reactive.Subjects;
using System.Collections.Immutable;
using Android.App;
using System.Threading;

namespace RxApp.Android
{
    public static partial class Bindings
    {
        public static IDisposable BindTo(this IObservable<NavigationStack> This, IRxApplication application, Action<Activity,INavigationViewModel> startActivity)
        {
            var activities = new Dictionary<INavigationViewModel, IViewFor>();

            // This is essentially an async lock. This code is single threaded so using a bool is ok.
            var canCreateActivity = RxProperty.Create(true);
            INavigationViewModel currentModel = null;

            var whenNavigationStackChanged = This
                .ObserveOnMainThread()
                .Delay(_ => canCreateActivity.Where(b => b))
                .Scan(RxApp.NavigationStack.Empty, (previous, navStack) =>
                    {
                        var removed = activities.Keys.Where(y => !navStack.Contains(y)).ToImmutableArray();
                        var previousActivity = activities[currentModel];
                        currentModel = navStack.FirstOrDefault();

                        if (currentModel != null && !activities.ContainsKey(currentModel))
                        {
                            canCreateActivity.Value = false;
                            startActivity((Activity) previousActivity, currentModel);
                        }

                        if (((int) AndroidBuild.VERSION.SdkInt >= 21) && !previous.IsEmpty && previous.Pop().Equals(navStack))
                        {
                            // Show the inverse activity transition after the back button is clicked.
                            ((Activity) previousActivity).FinishAfterTransition();
                        }
                        else
                        {
                            foreach (var model in removed)
                            {
                                IViewFor activity = activities[model];  
                                activities.Remove(model);
                                ((Activity) activity).Finish();
                            }   
                        }

                        return navStack;
                    });


            IDisposable subscription = null;

            return application.WhenActivityCreated.Subscribe(activity => 
                {
                    if (subscription == null)
                    {
                        // Either the startup activity called OnActivityCreated or the application was killed and restarted by android.
                        // If the application is backgrounded, android will kill all the activities and the application class.
                        // When the application is reopened from the background, it creates the application and starts the last activity 
                        // that was opened, not the startup activity. 
                        currentModel = new StartupModel();
                        activities[currentModel] = activity;

                        subscription = whenNavigationStackChanged
                            .Where(x => x.IsEmpty)
                            .Subscribe(x => 
                                {
                                    // Can't dispose the outer subscription from within its own callback, 
                                    // so post the call onto the sync context
                                    SynchronizationContext.Current.Post(_ => 
                                        {
                                            subscription.Dispose(); 
                                            subscription = null;
                                        }, null);
                                });
                    }
                    else
                    {
                        activity.ViewModel = currentModel;
                        activities[currentModel] = activity;
                        canCreateActivity.Value = true;
                    }
                });
        }

        private sealed class StartupModel : NavigationModel
        {
        }

        public static IDisposable BindTo<T, TView>(this IObservable<T> This, TView target, Expression<Func<TView, T>> property)
        {
            return This.BindTo(target, property, Scheduler.MainThreadScheduler);
        }

        public static IDisposable Bind(this IRxCommand This, Button button)
        {
            return Disposable.Compose(
                This.CanExecute.ObserveOnMainThread().Subscribe(x => button.Enabled = x),
                RxObservable.FromEventPattern(button, "Click").InvokeCommand(This)
            );
        }

        public static IDisposable Bind(this IRxProperty<bool> This, IMenuItem menuItem)
        {
            if (!menuItem.IsCheckable) { throw new ArgumentException("menuItem must be checkable"); }

            var clickListener = new ObservableOnMenuItemClickListener();
            menuItem.SetOnMenuItemClickListener(clickListener);

            return Disposable.Compose(
                This.BindTo(x => menuItem.SetChecked(x)),
                clickListener.Do(_ => menuItem.SetChecked(!menuItem.IsChecked)).Select(_ => menuItem.IsChecked).BindTo(This),
                RxDisposable.Create(() => menuItem.SetOnMenuItemClickListener(null)));
        }

        public static IDisposable Bind(this IRxCommand This, IMenuItem menuItem)
        {
            if (menuItem.IsCheckable) { throw new ArgumentException("menuItem must not be checkable"); }

            var clickListener = new ObservableOnMenuItemClickListener();
            menuItem.SetOnMenuItemClickListener(clickListener);

            return Disposable.Compose(
                This.CanExecute.BindTo(x => menuItem.SetEnabled(x)),
                clickListener.InvokeCommand(This),
                RxDisposable.Create(() => menuItem.SetOnMenuItemClickListener(null))
            );
        }

        private sealed class ObservableOnMenuItemClickListener : Java.Lang.Object, IMenuItemOnMenuItemClickListener, IObservable<Unit>
        {
            private readonly Subject<Unit> subject = new Subject<Unit>();

            public bool OnMenuItemClick(IMenuItem item)
            {
                subject.OnNext(Unit.Default);
                return true;
            }            

            public IDisposable Subscribe(IObserver<Unit> observer)
            {
                return subject.Subscribe(observer);
            }
        }

        public static IDisposable Bind(this IRxProperty<bool> This, CompoundButton button)
        {
            return Disposable.Compose(
                RxObservable.FromEventPattern<CompoundButton.CheckedChangeEventArgs>(button, "CheckedChange")
                          .Subscribe(x => { This.Value = x.EventArgs.IsChecked; }),

                This.ObserveOnMainThread().Subscribe(x => { if (button.Checked != x) { button.Checked = x; } }));
        }

        public static IDisposable BindTo(this IObservable<Unit> This, Action action)
        {
            return This.ObserveOnMainThread().Subscribe(_ => action());
        }

        public static IDisposable BindTo<T>(this IObservable<T> This, Action<T> action)
        {
            return This.ObserveOnMainThread().Subscribe(action);
        }

        public static IDisposable BindTo<TViewModel,TView>(
                this IObservable<IEnumerable<TViewModel>> This, 
                ListView listView, 
                Func<ViewGroup, TView> viewProvider, 
                Action<TViewModel, TView> bind)
            where TView:View
        {
            return This.Select(x => x.ToList()).BindTo(listView, viewProvider, bind);
        }

        public static IDisposable BindTo<TViewModel,TView>(
                this IObservable<IReadOnlyList<TViewModel>> This, 
                ListView listView, 
                Func<ViewGroup, TView> viewProvider, 
                Action<TViewModel, TView> bind)
            where TView:View
        {
            var adapter = new RxReadOnlyListAdapter<TViewModel, TView>(This, viewProvider, bind);
            listView.Adapter = adapter;

            return RxDisposable.Create(() =>
                {
                    listView.Adapter = null;
                    adapter.Dispose();
                });
        }

        private sealed class RxReadOnlyListAdapter<TViewModel, TView> : BaseAdapter<TViewModel>
            where TView : View
        {   
            private readonly IDisposable dataSubscription;
            private readonly Func<ViewGroup,TView> viewProvider;
            private readonly Action<TViewModel, TView> bind;

            private IReadOnlyList<TViewModel> list;

            public RxReadOnlyListAdapter(
                IObservable<IReadOnlyList<TViewModel>> data,
                Func<ViewGroup, TView> viewProvider, 
                Action<TViewModel, TView> bind)
            {
                list = new TViewModel[0]; 
                this.viewProvider = viewProvider;
                this.bind = bind;

                dataSubscription = 
                    data.ObserveOnMainThread()
                        .Do(x => list = x)
                        .Subscribe(_ => NotifyDataSetChanged());
            }

            public override long GetItemId(int position)
            {
                return list[position].GetHashCode();
            }

            private View GetView(int position, TView convertView, ViewGroup parent)
            {
                TView view = convertView;
            
                if (view == null)
                {
                    view = viewProvider(parent);
                }

                var viewModel = list[position];
                bind(viewModel, view);

                return view;
            }

            public override View GetView(int position, View convertView, ViewGroup parent)
            {
                TView theView = convertView != null ? (TView) convertView : null;
                return this.GetView(position, theView, parent);
            }

            public override bool HasStableIds { get { return true; } }

            public override int Count
            {
                get { return list.Count; }
            }

            public override TViewModel this [int index]
            {
                get { return list[index]; }
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    dataSubscription.Dispose();
                }
                base.Dispose(disposing);
            } 
        }
    }
}

