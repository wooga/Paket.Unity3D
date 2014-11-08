/// Application wide default values
module Paket.Unity3D.Constants

let [<Literal>] Unity3DReferencesFile = "paket.unity3d.references"
let [<Literal>] Unity3DCopyFolderName = "Paket.Unity3D"

let PluginDirs = ["iOS"; "Android"; "x86"; "x86_64";] 
                 |> List.map (fun d -> "Plugins/" + d)