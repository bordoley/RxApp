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
        T Value { set; }
    }

    public static class RxProperty
    {
        public static IRxProperty<T> Create<T>(T initialValue)
        {
            return new RxPropertyImpl<T>(initialValue);   
        }

        private class RxPropertyImpl<T> : IRxProperty<T>
        {
            private readonly BehaviorSubject<T> setValues;
            private readonly IObservable<T> values;

            internal RxPropertyImpl(T initialValue)
            {
                this.setValues = new BehaviorSubject<T>(initialValue);
                this.values = this.setValues.DistinctUntilChanged();
            }

            public T Value
            { 
                set { setValues.OnNext(value); }
            }

            public IDisposable Subscribe(IObserver<T> observer)
            {
                return values.Subscribe(observer);
            }

            public override string ToString()
            {
                // Behavior subject caches its most recent value so i think this is safe from deadlocks. Need to test.
                return string.Format("[RxPropertyImpl: Value={0}]", this.values.FirstAsync().Wait());
            }
        }
    }
}

