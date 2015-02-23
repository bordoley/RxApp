﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;

using Android.Text;
using Android.Views;
using Android.Widget;
using Android.Support.V4.Widget;
using System.Linq.Expressions;

using RxObservable = System.Reactive.Linq.Observable;
using RxDisposable = System.Reactive.Disposables.Disposable;

namespace RxApp.Android
{
    public static partial class Bindings
    {
        public static IDisposable BindTo<T, TView>(this IObservable<T> This, TView target, Expression<Func<TView, T>> property)
        {
            return This.BindTo(target, property, Observable.MainThreadScheduler);
        }

        public static IDisposable Bind(this IRxCommand This, Button button)
        {
            return Disposable.Compose(
                This.CanExecute.ObserveOnMainThread().Subscribe(x => button.Enabled = x),
                RxObservable.FromEventPattern(button, "Click").InvokeCommand(This)
            );
        }

        public static IDisposable Bind(this IRxCommand This, SwipeRefreshLayout refresher)
        {   
            return Disposable.Compose(
                RxObservable.FromEventPattern(refresher, "Refresh").Select(_ => Unit.Default)
                            .Subscribe(x => This.Execute()),
                This.CanExecute.ObserveOnMainThread().Subscribe(x => refresher.Enabled = x)
            );
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
            return This.ObserveOnMainThread().Subscribe(x => action(x));
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

