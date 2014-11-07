# Paket.Unity3D

An extension for the [Paket][11] dependency manager that enables the integration of NuGet dependencies into [Unity3D][12] projects.

## Why Paket.Unity3D?

While dependency managers like [NuGet][13] & [Paket][11] exist for .NET/Mono projects there is no easy way to manage dependencies for [Unity3D][12] projects.

Paket.Unity3D tries to solve this by adding NuGet libraries as Assets of a [Unity3D][12] project in a designated `Paket.Unity3D` directory.

Furthermore Paket.Unity3D works on the command-line and can be integrate into the build process.

## Online resources

 - [Source code][1]
 - [Documentation][2]
 - Download [paket.unity3d.exe][3]

[![NuGet Status](http://img.shields.io/nuget/v/Paket.Unity3D.svg?style=flat)](https://www.nuget.org/packages/Paket.Unity3D/)

## Troubleshooting and support

 - Found a bug or missing a feature? Feed the [issue tracker][4]
 - Announcements and related miscellanea through Twitter ([@PaketManager][5])

## Build status

|  |  BuildScript | Status of last build |
| :------ | :------: | :------: |
| **Mono** | [build.sh](https://github.com/devboy/Paket.Unity3D/blob/master/build.sh) | [![Travis build status](https://travis-ci.org/devboy/Paket.Unity3D.png)](https://travis-ci.org/devboy/Paket.Unity3D) |
| **Windows** | [build.cmd](https://github.com/devboy/Paket.Unity3D/blob/master/build.cmd) | [![Appveyor build status](https://ci.appveyor.com/api/projects/status/pbu35ledt76viqmj/branch/master?svg=true)](https://ci.appveyor.com/project/devboy/paket-unity3d/branch/master)

## Quick contributing guide

 - Fork and clone locally.
 - Build the solution with Visual Studio, `build.cmd` or `build.sh`.
 - Create a topic specific branch in git. Add a nice feature in the code. Do not forget to add tests and/or docs.
 - Run `build.cmd` (`build.sh` on Mono) to make sure all tests are still passing.
 - Send a Pull Request.

If you want to contribute to the [docs][2] then please modify the markdown files in `/docs/content` and send a pull request.

## License

The [MIT license][6]

 [1]: https://github.com/devboy/Paket.Unity3D/
 [2]: http://devboy.github.io/Paket.Unity3D/
 [3]: https://github.com/devboy/Paket.Unity3D/releases/latest
 [4]: https://github.com/devboy/Paket.Unity3D/issues
 [6]: https://github.com/devboy/Paket.Unity3D/blob/master/LICENSE.txt
 [11]: http://fsprojects.github.io/Paket
 [12]: http://unity3d.com/
 [13]: http://www.nuget.org
