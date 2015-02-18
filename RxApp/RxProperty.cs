using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;

namespace RxApp
{
    public interface IRxProperty<T> : IObservable<T>
    {
        T Value { get; set; }
    }

    public static class RxProperty
    {
        public static IRxProperty<T> Create<T>(T initialValue)
        {
            var subject = new BehaviorSubject<T>(initialValue);
            return new RxPropertyImpl<T>(subject);   
        }

        public static IDisposable BindTo<T>(this IObservable<T> This, IRxProperty<T> property)
        {
            return This.Subscribe(t => { property.Value = t; });
        }

        private class RxPropertyImpl<T> : IRxProperty<T>
        {
            private readonly BehaviorSubject<T> setValues;
            private readonly IObservable<T> values;

            internal RxPropertyImpl(BehaviorSubject<T> setValues)
            {
                this.setValues = setValues;
                this.values = this.setValues.DistinctUntilChanged();
            }

            public T Value
            { 
                get { return setValues.Value; }
                set { setValues.OnNext(value); }
            }

            public IDisposable Subscribe(IObserver<T> observer)
            {
                return values.Subscribe(observer);
            }

            public override string ToString()
            {
                return string.Format("[RxPropertyImpl: Value={0}]", this.setValues.Value);
            }
        }
    }
}

