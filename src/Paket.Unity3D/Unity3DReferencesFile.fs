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

module Utils =
    /// Gets all dirs with the given pattern
    let inline FindAllDirs(folder, pattern) = DirectoryInfo(folder).GetDirectories(pattern, SearchOption.AllDirectories)
    let inline (+/) a b = Path.Combine(a,b)

open Utils

module UnityProject =
    let FindAllProjects folder =
        FindAllDirs(folder, "Assets")
        |> Array.filter (fun d -> FileInfo(d.Parent.FullName+/"ProjectSettings"+/"ProjectSettings.asset").Exists)
        |> Array.map (fun d -> d.Parent)

    let FindOrCreateReferencesFile (dir : DirectoryInfo) =
        let fi = FileInfo(dir.FullName +/ Constants.Unity3DReferencesFile)
        if not fi.Exists then fi.Create().Close()
        fi.FullName |> Paket.ReferencesFile.FromFile
        

/// Represents paket.unity3d.references file
module ReferencesFile = 
    let private ProjectReferenceFile (r:FileInfo) =
        let proj x = Project(x)
        try r.FullName |> Paket.ReferencesFile.FromFile |> proj |> succeed
        with _ -> fail "Project not valid"
            
    let FindAllReferencesFiles(folder) =
        FindAllFiles(folder, Constants.Unity3DReferencesFile) 
        |> Seq.map ProjectReferenceFile
