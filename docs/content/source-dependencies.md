# Source dependencies

To distribute source-code and/or arbitrary files with Paket.Unity3D the [content files][contentfiles] feature of NuGet is used.

## File copy

Files and folders inside the `content` directory of a nuget package will be copied to `Assets/Paket.Unity3D/$PKG_NAME/`.

## Plugin directories

Paket.Unity3D respects the `Plugins` directory which means that it will be copied to `Assets/Plugins`.

[contentfiles]: http://docs.nuget.org/docs/reference/nuspec-reference#Content_Files
