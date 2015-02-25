# Quick start guide

This guide will focus on Paket.Unity3D and you will need a `.paket` directory containing a recent version of [paket.exe][paket.exe] at the root of your project directory structure.
Please also have a look at the documentation of [Paket][paket].

## paket.dependencies

If no `paket.dependencies` file is present in the root of your project you can create one as follows:

		[lang=batchfile]
		[mono] .paket/paket.exe init

First you need to add Paket.Unity3D as a dependency. So ensure that the following `source` and `nuget` dependency are present in `paket.dependencies`:

		source https://nuget.org/api/v2
		nuget Paket.Unity3D

Proceed by installing Paket.Unity3D:

		[lang=batchfile]
		[mono] .paket.exe update

`paket.unity3d.exe` should now be present under `./packages/Paket.Unity3D/tools/`

For more info on `paket.dependencies` check the [Paket documentation][paket.dependencies]

## paket.unity3d.references

Then add a `paket.unity3d.references` file next to the `Assets` directory of your Unity3D project.

Declare your dependencies, for example:

In `paket.dependencies`:

    nuget Paket.Unity3D.Example.Source

In `paket.unity3d.references`:

		Paket.Unity3D.Example.Source

`paket.unity3d.references` is the same as a `paket.references` file. For more info on `paket.dependencies` check the [Paket documentation][paket.references]

## Update and install

Update your dependencies with [Paket][paket]

    [lang=batchfile]
    $ [mono] .paket/paket.exe update

Install the references into the Unity3D project

    [lang=batchfile]
    $ [mono] packages/Paket.Unity3D/tools/paket.unity3d.exe

Your dependencies should now be added to the Assets directory.

[paket.dependencies]: http://fsprojects.github.io/Paket/dependencies-file.html
[paket.references]: http://fsprojects.github.io/Paket/references-files.html
[paket]: http://fsprojects.github.io/Paket/
[paket.exe]: https://github.com/fsprojects/Paket/releases/latest
