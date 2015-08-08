# Quick start guide

This guide will show you how to use **Paket.Unity3D** and make NuGet dependencies available in your Unity3D project.

## Unity3D project

Given a Unity3D project named `YourAwesomeGame` with the following directory structure:

	[lang=batchfile]
	.
	└── YourAwesomeGame
	    ├── Assets
	    ├── Library
	    ├── ProjectSettings
	    └── Temp

## Installation

You will need to install both **Paket** and **Paket.Unity3D**. The simplest way is to create a `.paket` directory next to your `Assets` directory and download the bootstrapper executables.

`paket.bootstrapper.exe` can be found at [Paket releases](https://github.com/fsprojects/Paket/releases/latest)

`paket.unity3d.bootstrapper.exe` can be found at [Paket.Unity3D releases](https://github.com/wooga/Paket.Unity3D/releases/latest)

Once you've got both files in your `.paket` directory, fire up a shell and run both bootstrappers:

	[lang=batchfile]
	$ [mono] .paket/paket.bootstrapper.exe
	$ [mono] .paket/paket.unity3d.bootstrapper.exe

This will download the latest executables of **Paket** and **Paket.Unity3D** into your `.paket` directory.

## Initialization

You will have to create a file called `paket.dependencies` with the following content:

	[lang=batchfile]
	source https://nuget.org/api/v2

[More on paket.dependencies](http://fsprojects.github.io/Paket/dependencies-file.html)

## Adding a nuget dependency

To add a NuGet dependency, `Wooga.Lambda` is used as an example here, append the following to your `paket.dependencies` file:

	[lang=batchfile]
	nuget Wooga.Lambda

[More on NuGet dependencies](http://fsprojects.github.io/Paket/nuget-dependencies.html)

## Downloading the dependency

To actually download `Wooga.Lambda` you'll need to run:

	[lang=batchfile]
	$ [mono] .paket/paket.exe update

[More on paket update](http://fsprojects.github.io/Paket/paket-update.html)

## Referencing the dependency

You will have to create a file called `paket.unity3d.references` next to your `Assets` directory with the following content:

	[lang=batchfile]
	Wooga.Lambda

[More on paket.unity3d.references](http://wooga.github.io/Paket.Unity3D/references-files.html)

## Installing the dependency

Once the dependency is downloaded you will want to actually add it to your Unity3D project by running:

	[lang=batchfile]
	$ [mono] .paket/paket.unity3d.exe install

Your project directory should now look like this:

	[lang=batchfile]
	.
	└── YourAwesomeGame
	├── Assets
	│   └── Paket.Unity3D
	│       └── Wooga.Lambda
	│           └── Wooga.Lambda.dll
	├── Library
	│   └── [...]
	├── ProjectSettings
	│   └── [...]
	├── Temp
	│   └── [...]
	├── packages
	│   └── Wooga.Lambda
	│       └── [...]
	├── paket.dependencies
	├── paket.lock
	└── paket.unity3d.references
