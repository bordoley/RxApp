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
    public interface IRxProperty<T> : IObservable<T>, IDisposable
    {
        T Value { get; set; }
    }

    public static class RxProperty
    {
        public static IRxProperty<T> Create<T>(T initialValue)
        {
            return new RxPropertyImpl<T>(initialValue);   
        }

        private class RxPropertyImpl<T> : IRxProperty<T>
        {
            private readonly Subject<T> setValues = new Subject<T>();
            private readonly IObservable<T> values;
            private readonly IDisposable valuesDisp;

            private T value;

            internal RxPropertyImpl(T initialValue)
            {
                this.value = initialValue;

                var values = this.setValues.DistinctUntilChanged().Do (x => { this.value = x; }).Publish();
                this.valuesDisp = values.Connect();
                this.values = values;
            }

            public T Value
            { 
                get { return value; } 
                set { setValues.OnNext(value); }
            }

            public IDisposable Subscribe(IObserver<T> observer)
            {
                return values.StartWith(this.value).DistinctUntilChanged().Subscribe(observer);
            }

            public void Dispose()
            {
                this.valuesDisp.Dispose();
            }
        }
    }
}

