/// Handles the installation of dependencies into Unity3D projects
module Paket.Unity3D.InstallProcess

open Paket
open Paket.Logging
open System.IO
open System.Collections.Generic

let private cleanTargetDirectories (unityProject:Unity3DReferencesFile) =
    Constants.PluginDirs
    |> List.map (fun pd -> Path.Combine(unityProject.UnityAssetsDir, pd, Constants.Unity3DCopyFolderName))
    |> List.append [Path.Combine(unityProject.UnityAssetsDir, Constants.Unity3DCopyFolderName)]
    |> List.iter (fun dir -> Utils.CleanDir dir)

let private findPackagesWithContent (usedPackages:Dictionary<_,_>) = 
    usedPackages
    |> Seq.map (fun kv -> kv.Key, DirectoryInfo(Path.Combine("packages", kv.Key, "content")))
    |> Seq.filter (fun (package,packageDir) -> packageDir.Exists)
    |> Seq.toList

let private copyContentFiles (project : Unity3DReferencesFile, package, dir) = 
    
    let root:DirectoryInfo = dir
    
    let isPluginDir (dir:DirectoryInfo) = 
        Constants.PluginDirs
        |> List.map (fun pd -> Path.Combine(root.FullName, pd))
        |> List.exists ((=) dir.FullName)                 

    let rules : list<(FileInfo -> bool)> = [
            fun f -> f.Name = "_._"
            fun f -> f.Name.EndsWith(".transform")
            fun f -> f.Name.EndsWith(".pp")
            fun f -> f.Name.EndsWith(".tt")
            fun f -> f.Name.EndsWith(".ttinclude")
        ]
    
    let onBlackList (fi : FileInfo) = rules |> List.exists (fun rule -> rule(fi))

    let rec copyDirContents (fromDir : DirectoryInfo, toDir : Lazy<DirectoryInfo>) =
        fromDir.GetDirectories()
        |> Array.filter (fun d -> not( isPluginDir d )) 
        |> Array.toList
        |> List.collect (fun subDir -> copyDirContents(subDir, lazy toDir.Force().CreateSubdirectory(subDir.Name)))
        |> List.append
            (fromDir.GetFiles() 
                |> Array.toList
                |> List.filter (onBlackList >> not)
                |> List.map (fun file -> file.CopyTo(Path.Combine(toDir.Force().FullName, file.Name), true)))

    let targetDir = DirectoryInfo(Path.Combine(project.UnityAssetsDir, Constants.Unity3DCopyFolderName, package))

    // Copy content files
    copyDirContents (root, lazy (targetDir)) |> ignore
    // Copy plugin content files
    Constants.PluginDirs
    |> List.map (fun pd -> pd, DirectoryInfo(Path.Combine(root.FullName, pd)))
    |> List.filter (fun (plugin, source) -> source.Exists )
    |> List.map (fun (plugin,source) -> source, DirectoryInfo(Path.Combine(project.UnityAssetsDir, plugin, Constants.Unity3DCopyFolderName, package)) )
    |> List.iter (fun (source, target) -> 
        Utils.CleanDir target.FullName
        copyDirContents(source, lazy(target)) |> ignore )    

/// Installs Paket dependencies into the Unity3D Assets directory
let Install(sources,force, hard, lockFile:LockFile) =
    let extractedPackages = Paket.InstallProcess.createModel(sources,force, lockFile)

    let model =
        extractedPackages
        |> Array.map (fun (p,m) -> p.Name.ToLower(),m)
        |> Map.ofArray

    let applicableProjects =
        Paket.Unity3D.Unity3DReferencesFile.FindAllReferencesFiles(".")
           
    for unityProject in applicableProjects do    
        verbosefn "Installing to %s" unityProject.UnityAssetsDir

        cleanTargetDirectories unityProject

        let usedPackages = lockFile.GetPackageHull(unityProject.ReferencesFile)
        
        usedPackages
        |> Seq.map (fun kv -> 
                            let installModel = model.[kv.Key.ToLower()]
                            let dlls = installModel.GetFiles(DotNetFramework(FrameworkVersion.V3_5))
                                       |> Seq.map (fun f -> FileInfo(f)) 
                            let path =Path.Combine(unityProject.UnityAssetsDir, Constants.Unity3DCopyFolderName, installModel.PackageName)
                            path, dlls )
        |> Seq.iter (fun (path,dlls) -> Utils.CleanDir path
                                        for dll in dlls do dll.CopyTo(Path.Combine(path, dll.Name)) |> ignore )

        usedPackages
        |> findPackagesWithContent
        |> Seq.iter (fun (package,dir) -> copyContentFiles(unityProject,package,dir) )
           
    ()