RxApp
=====

RxApp is a functional reactive MVVM framework for building mobile applications base upon the .NET 
[Reactive Extensions](https://github.com/Reactive-Extensions/Rx.NET).

Combining a functional reactive data binding framework with a view model driven 
navigation framework, RxApp allows developers to build truly testable cross platform
mobile applications.

# Supported Platform
  * Portable Class Libraries (Profile 259)
  * Xamarin.iOS
  * Xamarin.Android
  * Xamarin.Forms

# How do I add RxApp to my project?

Use the NuGet packages:

# What are RxApp's dependencies?

The core platform only depends on the .NET [Reactive Extensions](https://github.com/Reactive-Extensions/Rx.NET)
and [Immutable Collections](https://github.com/dotnet/corefx/tree/master/src/System.Collections.Immutable).

The platform specific UI bindings only introduce dependencies on their native frameworks.

# How stable is RxApp?

RxApp is under active development and is not yet API stable. Its currently released as a pre-release on Nuget. While there may be cosmetic changes to the library (propery name changes, etc.), I don't expect any significant architectural changes to the current API. Future releases will primarily focus on additional control binding support across all platforms and the addition of support for new platforms, specifically Xamarin.Mac, WPF, Windows Phone and Windows Store apps.

# Building an application using RxApp

## ViewModels

## Platform agnostic business logic

## Platform specific UI databinding
