using System;
using System.Reactive.Concurrency;

namespace RxApp.XamarinForms
{
    internal static partial class Scheduler
    {   
        internal static IScheduler MainThreadScheduler 
        { 
            get { return RxApp.iOS.Scheduler.MainThreadScheduler; } 
        } 
    }
}

