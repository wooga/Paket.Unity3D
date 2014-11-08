# Quick start guide

Go over to [Paket][11] and install it into your project.

## paket.dependencies

Proceed by adjusting your `paket.dependencies` file to include `Paket.Unity3D`. We use `LibNoise` as an example dependency to be installed in the Unity3D project

    source https://nuget.org/api/v2

    nuget LibNoise
    nuget Paket.Unity3D

For more info on `paket.dependencies` check the [Paket documentation][1]

## paket.unity3d.references

Then add a `paket.unity3d.references` next to the `Assets` directory of the Unity3D project which contains the `LibNoise` reference.

    LibNoise

## Update and install

Update your dependencies with [Paket][11]

    [lang=batchfile]
    $ [mono] .paket/paket.exe update

Install the references into the Unity3D project

    [lang=batchfile]
    $ [mono] packages/Paket.Unity3D/tools/paket.unity3d.exe

LibNoise is now added to the Assets directory and the directory should look like this

![alt text](img/quick-start-folders.png "Directory structure")

Now LibNoise can be accessed in by other code in the Unity3D project.

[1]: http://fsprojects.github.io/Paket/dependencies-file.html
[11]: http://fsprojects.github.io/Paket
