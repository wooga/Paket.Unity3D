namespace System
open System.Reflection

[<assembly: AssemblyTitleAttribute("Paket.Unity3D")>]
[<assembly: AssemblyProductAttribute("Paket.Unity3D")>]
[<assembly: AssemblyDescriptionAttribute("Piggy-backs ontop of Paket to add dependencies to Unity3D projects")>]
[<assembly: AssemblyVersionAttribute("1.0")>]
[<assembly: AssemblyFileVersionAttribute("1.0")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "1.0"
