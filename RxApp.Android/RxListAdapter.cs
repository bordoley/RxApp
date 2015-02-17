using System;
using Android.Views;
using Android.Widget;

namespace RxApp
{
    public static class RxAdapter
    {
        public static IListAdapter GetAdapter<TViewModel, TView>(
                this IRxReadOnlyList<TViewModel> This, 
                Func<TViewModel, ViewGroup, TView> viewCreator, 
                Action<TViewModel, TView> bindModelToView)
            where TViewModel : class
            where TView : View
    
        {
            return new ReactiveListAdapter<TViewModel, TView>(This, viewCreator, bindModelToView);
        }

        private sealed class ReactiveListAdapter<TViewModel, TView> : BaseAdapter<TViewModel>
            where TViewModel : class
            where TView : View
        {
            private readonly IRxReadOnlyList<TViewModel> list;
            private readonly Func<TViewModel, ViewGroup, TView> viewCreator;
            private readonly Action<TViewModel, TView> bindModelToView;

            private IDisposable _inner;

            public ReactiveListAdapter(
                IRxReadOnlyList<TViewModel> backingList,
                Func<TViewModel, ViewGroup, TView> viewCreator,
                Action<TViewModel, TView> bindModelToView)
            {
                this.list = backingList;
                this.viewCreator = viewCreator;
                this.bindModelToView = bindModelToView;

                _inner = this.list.Changed.Subscribe(_ => NotifyDataSetChanged());
            }

            public override TViewModel this[int index] { get { return list[index]; } }

            public override long GetItemId(int position) { return list[position].GetHashCode(); }

            public override bool HasStableIds { get { return true; } }

            public override int Count { get { return list.Count; } }

            private View GetView(int position, TView convertView, ViewGroup parent)
            {
                TView theView = convertView;
                var data = list[position];

                if (theView == null)
                {
                    theView = viewCreator(data, parent);
                }
    
                bindModelToView(data, theView);

                return theView;
            }

            public override View GetView(int position, View convertView, ViewGroup parent)
            {
                TView theView = convertView != null ? (TView) convertView : null;
                return this.GetView(position, theView, parent);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _inner.Dispose();
                }

                base.Dispose(disposing);
            }
        }
    }
}