# Paket.Unity3D

Use Paket and NuGet dependencies in Unity3D projects (take a look at:http://fsprojects.github.io/Paket)

* add Paket.Unity3D to your paket.dependencies `nuget Paket.Unity3D == 0.0.3-beta`
* place a `paket.unity3d.references` file next to your Unity3D projects `Assets` directory
* run paket as you would for .Net projects to gather the dependencies in your paket.dependencies file
* run `[mono] packages/Paket.Unity3D/tools/paket.unity3d.exe` from the root of your project to install dependencies into your Unity3D project(s)

Documentation: http://devboy.github.io/Paket.Unity3D
