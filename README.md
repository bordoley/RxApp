RxApp
=====

RxApp is a functional reactive MVVM framework for building mobile applications base upon the .NET 
[Reactive Extensions](https://github.com/Reactive-Extensions/Rx.NET).

Combining a functional reactive data binding framework with a view model driven 
navigation framework, RxApp allows developers to build truly testable cross platform
mobile applications.

# Supported Platforms
  * Portable Class Libraries (Profile 259)
  * Xamarin.iOS
  * Xamarin.Android
  * Xamarin.Forms

# How do I add RxApp to my project?

Use the NuGet packages:

# What are RxApp's dependencies?

The core platform depends on the .NET [Reactive Extensions](https://github.com/Reactive-Extensions/Rx.NET)
and [Immutable Collections](https://github.com/dotnet/corefx/tree/master/src/System.Collections.Immutable).

The platform specific UI bindings introduce dependencies upon their native frameworks.

# How stable is RxApp?

RxApp is under active development and is not yet API stable. Its currently released as a pre-release on 
Nuget. While there may be cosmetic changes to the library (propery name changes, etc.), I don't expect any
significant architectural changes to the current API. Future releases will primarily focus on additional 
control binding support across all platforms and the addition of support for new platforms, specifically 
Xamarin.Mac, WPF, Windows Phone and Windows Store apps.

# Building an application using RxApp

The style of MVVM used in RxApp is slightly different than what you might familiar with. The best way to understand
and contrast the differences is to consider an example. Lets build a login dialog that you might encounter in
a typical mobile application. Within such a UI, you will typically have text entry boxes for the username and 
password fields, along with a button for the user to complete the login process. In addition, while the application 
is attempting to log the user in, you typically will display a progress indicator to the user to indicate that the 
applications is busy and attempting to complete the login process. 

## A quick overview of traditional MVVM

In traditional .NET MVVM applications, view models expose public mutable properties and ICommands, 
and notify of property changes by implementing the INotifyPropertyChanged interface. The UI framework
listens to propery changes on the view model and updates the UI state when they change, as well as 
mutating properties on the view model when the UI state changes.

Lets consider our login example. One implementation might look like:

```CSharp
public sealed class TraditionalLoginViewModel : INotifyPropertyChanged
{
    private readonly ICommand loginCommand = new RelayCommand(param => this.LogInToApp());

    public string UserName { get; set; }
    public string Password { get; set; }
    public bool LogginIn { get; set; }

    public ICommand DoLogin { get { return loginCommand; }

    private async Task LogInToApp()
    {
        var username = this.UserName;
        var password = this.Password;
        this.LogginIn = true;

        bool loginResult = await CallLoginService(username, password);

        if (loginResult)
        {
            // navigate to the next state on login success
        }
        else
        {
            // pop up the error dialog
        }
    }
}
```

Within a traditional view model, developers implement logic that interacts with underlying system
data models and services, such as web service APIs, SQLite databases etc., updating the view model state in response to user actions.

While this is an improvement over other models, such as MVC, it still introduces complex mutable state management 
into the system, and forces developers to carefully consider concurrency and the impacts of threading to prevent 
updates to the view model from non-UI threads. In addition, the traditional design combines business logic with the 
view model, which introduces coupling and makes testing harder than it has to be.

## View Models

In contrast to the tradional design, View Models in RxApp can be thought of as collections of properties and include virtually no logic. 

For instance consider the design of a basic UI to support a login dialog.
Within RxApp, we design our view model to directly mimic our desired logical user interface. 

```CSharp
public interface ILoginViewModel : INavigationViewModel
{
    IRxProperty<string> UserName { get; }
    IRxProperty<string> Password { get; }
    IRxCommand LoginModel { get; }

    IObservable<bool> LoggingIn { get; }
}
```

You'll notice several things going on here. 

First you'll notice, that we don't expose mutable properties as in the traditional design and don't impplement 
INotifyPropertyChanged. Instead we expose instances of IRxProperty, IRxCommand, and IObservable. 

Let's dig into the details of IRxProperty and IRxCommand a bit deeper. 

  * Unlike traditional mutable properties, IRxProperty instances can be thgought of like streams of values, whose 
    current value can be always be retrieved by subscribing to the the property. A value can be imperatively 
    published to the properties listeners by setting the IRxProperty.Value property. However unlike a mutable
    property on the traditional view model, setting the property is thread safe and may be set from any thread. 
    This is guaranteed by the underlying Rx BehaviorSubject used to implement the property.

  * IRxCommand is an improvement over the traditional ICommand interface, but does not itself implement ICommand. 
    In contrast to ICommand, IRxCommand implement IObservable. Specifically there are two command variants 
    in RxApp: IRxCommand which implements ```IObseverable<Unit>```, and ```IRxCommand<T>``` 
    which implements ```IObservable<Unit>```. Normally you will use the non-generic version which is designed for 
    databinding button clicks etc., but occasionally you will run into situations where you will need the ability 
    to fire and forget data. Consider carefully whether using an IRxProperty would work better first. 

In addition, we are defining our view models in terms of an interface. 
While not strictly required in RxApp, doing so is very useful. This design clearly denotes what the shape of the 
view model from differing perspective of its users and consumers. For instance, ILoginViewModel denotes an inteface 
that directly mimics the interface that the view would expose to the user. But what about the consumers of the view model data? In RxApp we will typically expose an additional controller interface. For instance for our login view model we'd expose:

```CSharp
public interface ILoginControllerModel : INavigationControllerModel
{
    IObservable<string> UserName { get; }
    IObservable<string> Password { get; }
    IObservable<Unit> DoLogin { get; }

    IRxProperty<bool> LoggingIn { get; }
    IRxCommand LoginFailed { get; }
}
```
In contrast, ILoginControllerModel exposes the view of the model from the perspective of the application which will 
consume the user data and take action on behalf the user.

Finally we'll expose an implementation class that implements both interfaces:
```CSharp
public sealed class LoginModel : NavigationModel, ILoginViewModel, ILoginControllerModel
{
}
```

## View Model Driven Navigation

## Platform Agnostic Business Logic
RxApp's view model driven navigation dynamically binds view models to controllers that consume the view model data and take action on behalf of the user

```CSharp
public static IDisposable Create(ILoginControllerModel model)
{
    model.DoLogin
        // Get the most recent username and password from the view model
        .SelectMany(_ =>  RxAppObservable.CombineLatest(model.UserName, model.Password).FirstAsync()) 

        // Indicate that we are logging in. This will cause the ui to show a progress dialog
        .Do(_ => model.LoggingIn.Value = true)

        // Actually log in
        .SelectMany(async x => await DoLogin(x.Item1, x.Item2))

        // React to the result of logging in.
        .Do(loginSucceed =>
            {
                if (loginSucceed)
                {
                    // Login succeeded navigate to the main page
                    model.Open.Execute(new MainPageModel());
                } 
                else 
                {
                    // Notify the user that loggin failed
                    model.LoginFailed.Execute();

                    // Indicate that we are no longer logging in
                    model.LoggingIn = false;
                }
            })
        .Subscribe();
}
```

## Platform specific UI databinding

Finally now that we've implemented our view model and have binded it to our business logic, its time to hook it up to our UI. I'm an Android fan, so lets build an Android Activity (the code is nearly identical for iOS, just subsitute RxUIViewcontroller for RxActionBarActivity). Notice how our databinding is purely declarative. We bind UI properties or elements to IRxProperty, IRxCommand and IObservable properties on our ViewModel, but have virtuall no logic in the bindings.

```CSharp
[Activity(Label = "MainActivity")]            
public sealed class MainActivity : RxActionBarActivity<ILoginViewModel>
{
    private IDisposable subscription = null;

    private Button loginButton;
    private EditText userName;
    private EditTest password;

    protected override void OnCreate(Bundle bundle)
    {
        // Update the activity theme. Must be the first thing done in OnCreate();
        this.SetTheme(Resource.Style.RxAppTheme);
        base.OnCreate(bundle);

        this.SetContentView(Resource.Layout.Main);

        loginButton = this.FindViewById<Button>(Resource.Id.LoginButton);
        userName = FindViewById<EditText>(Resource.Id.UserName);
        password = FindViewById<EditText>(Resource.Id.PassWord);
    }

    protected override void OnStart()
    {
        base.OnStart();

        // Set up the data bindings
        subscription = Disposable.Compose(
            // Two way bind the login button to the DoLogin IRxCommand
            this.ViewModel.DoLogin.Bind(button),

            // Set the username and password properties a half second after they were last changed
            Observable.FromEventPattern(this.userName, "AfterTextChanged")
                          .Throttle(TimeSpan.FromSeconds(.5))
                          .Select(x => userName.Text)
                          .BindTo(this.ViewModel.UserName),

            Observable.FromEventPattern(this.password, "AfterTextChanged")
                          .Throttle(TimeSpan.FromSeconds(.5))
                          .Select(x => password.Text)
                          .BindTo(this.ViewModel.Password),

            // If login failed show a toast to the user
            this.ViewModel.LoginFailed
                    .BindTo(Toast.MakeText(
                                this, 
                                this.Resources.GetString(Resource.String.login_failed), 
                                ToastLength.Long).Show)
        );
    }

    protected override void OnStop()
    {
        subscription.Dispose();
        base.OnStop();
    }
}
```

