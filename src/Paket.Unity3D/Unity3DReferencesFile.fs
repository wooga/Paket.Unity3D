/// Utilities to install Paket dependencies into Unity3D projects 
namespace Paket.Unity3D

open Paket.Utils
open System.IO
open Paket.Rop

type Project(references:Paket.ReferencesFile) =
    member this.References = references
    member this.Directory = FileInfo(this.References.FileName).Directory
    member this.Name = this.Directory.Name
    member this.Assets = DirectoryInfo(Path.Combine(this.Directory.FullName,"Assets"))
    member this.PaketDirectory = DirectoryInfo(Path.Combine(this.Assets.FullName,Constants.Unity3DCopyFolderName))

/// Represents paket.unity3d.references file
module ReferencesFile = 
    let private ProjectReferenceFile (r:FileInfo) =
        let proj x = Project(x)
        try r.FullName |> Paket.ReferencesFile.FromFile |> proj |> succeed
        with _ -> fail "Project not valid"
            
    let FindAllReferencesFiles(folder) =
        FindAllFiles(folder, Constants.Unity3DReferencesFile) 
        |> Seq.map ProjectReferenceFile
