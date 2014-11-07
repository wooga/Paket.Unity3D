# What is Paket.Unity3D?

An extension for the [Paket][paket] dependency manager that enables the integration of NuGet dependencies into [Unity3D][unity] projects.

  [paket]: http://fsprojects.github.io/Paket/
  [nuget]: https://www.unity3d.com/

## How to get Paket.Unity3D

Paket.Unity3D is available as:

  * [download from GitHub.com](https://github.com/devboy/Paket.Unity3D/releases/latest)
  * as a package [`Paket` on nuget.org](https://www.nuget.org/packages/Paket.Unity3D/)

[![NuGet Status](http://img.shields.io/nuget/v/Paket.Unity3D.svg?style=flat)](https://www.nuget.org/packages/Paket.Unity3D/)

## Getting Started

Please have a look at the documentation of [Paket][paket] on how to setup and declare project dependencies.

Place the `paket.unity3d.exe` next to your `paket.exe`

You can place `paket.unity3d.references` alongside your Unity3D `Assets` directory to have Paket.Unity3D automatically sync files for the packages noted in that file whenever `paket.unity3d.exe` is executed.

TODO: Create sample tutorial

Contributing and copyright
--------------------------

The project is hosted on [GitHub][gh] where you can [report issues][issues], fork the project and submit pull requests.

Please see the [Quick contributing guide in the README][readme] for contribution gudelines.

The library is available under MIT license, which allows modification and redistribution for both commercial and non-commercial purposes.
For more information see the [License file][license].

  [content]: https://github.com/fsprojects/Paket.Unity3D/tree/master/docs/content
  [gh]: https://github.com/devboy/Paket.Unity3D
  [issues]: https://github.com/devboy/Paket.Unity3D/issues
  [readme]: https://github.com/devboy/Paket.Unity3D/blob/master/README.md
  [license]: http://devboy.github.io/Paket.Unity3D/license.html
