using Android.App;
using Android.Content;
using Android.Views;

namespace RxApp.Android
{  
    public interface IApplication 
    {
        Context ApplicationContext { get; }

        void OnCreate();
        void OnTerminate();
    }

    public interface IActivity 
    {
        Application Application { get; }
        FragmentManager FragmentManager { get; }
        void Finish();
    }
}