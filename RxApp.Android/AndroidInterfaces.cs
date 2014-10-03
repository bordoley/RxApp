using Android.App;
using Android.Content;
using Android.Views;

namespace RxApp
{  
    public interface IAndroidApplication 
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
        bool OnOptionsItemSelected(IMenuItem item);
    }
}