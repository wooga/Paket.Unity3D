/// Application wide default values
module Paket.Unity3D.Constants

open System.IO

let [<Literal>] Unity3DReferencesFile = "paket.unity3d.references"
let [<Literal>] Unity3DCopyFolderName = "Paket.Unity3D"
let Unity3DDotNetCompatibiliy = Paket.FrameworkIdentifier.DotNetFramework(Paket.FrameworkVersion.V3_5)

let PluginDirs = ["iOS"; "Android"; "x86"; "x86_64";] 
                 |> List.map (fun x -> Path.Combine("Plugins",x))