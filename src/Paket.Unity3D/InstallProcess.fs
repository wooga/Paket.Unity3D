module Paket.Unity3D.InstallProcess

open Paket
open Paket.Logging
open System.IO
open System.Collections.Generic

let private findPackagesWithContent (usedPackages:Dictionary<_,_>) = 
    usedPackages
    |> Seq.map (fun kv -> kv.Key, DirectoryInfo(Path.Combine("packages", kv.Key, "content")))
    |> Seq.filter (fun (package,packageDir) -> packageDir.Exists)
    |> Seq.toList

let private copyContentFiles (project : Unity3DReferencesFile, package, dir) = 
    
    let root:DirectoryInfo = dir
    let androidSourceDir = DirectoryInfo(Path.Combine(root.FullName, Constants.UnityAndroidPluginPath))
    let iOSSourceDir = DirectoryInfo(Path.Combine(root.FullName, Constants.UnityIOSPluginPath))

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
        |> Array.filter (fun d -> verbosefn "d: %A" d.FullName
                                  verbosefn "a: %A" androidSourceDir.FullName
                                  verbosefn "i: %A" iOSSourceDir.FullName 
                                  not(d.FullName = androidSourceDir.FullName) && not(d.FullName = iOSSourceDir.FullName )) 
        |> Array.toList
        |> List.collect (fun subDir -> copyDirContents(subDir, lazy toDir.Force().CreateSubdirectory(subDir.Name)))
        |> List.append
            (fromDir.GetFiles() 
                |> Array.toList
                |> List.filter (onBlackList >> not)
                |> List.map (fun file -> file.CopyTo(Path.Combine(toDir.Force().FullName, file.Name), true)))

    let targetDir = DirectoryInfo(Path.Combine(project.UnityAssetsDir, Constants.Unity3DCopyFolderName, package))
    let androidTargetDir = DirectoryInfo(Path.Combine(project.UnityAssetsDir, Constants.UnityAndroidPluginPath, Constants.Unity3DCopyFolderName, package))
    let iOSTargetDir = DirectoryInfo(Path.Combine(project.UnityAssetsDir, Constants.UnityIOSPluginPath, Constants.Unity3DCopyFolderName, package)) 

    copyDirContents (root, lazy (targetDir)) |> ignore
    if androidSourceDir.Exists then
        Utils.CleanDir androidTargetDir.FullName 
        copyDirContents(androidSourceDir, lazy(androidTargetDir)) |> ignore
    if iOSSourceDir.Exists then
        Utils.CleanDir iOSTargetDir.FullName 
        copyDirContents(iOSSourceDir, lazy(iOSTargetDir)) |> ignore

let Install(sources,force, hard, lockFile:LockFile) =
    let extractedPackages = Paket.InstallProcess.createModel(sources,force, lockFile)

//    verbosefn "extractedPackages: %A" extractedPackages

    let model =
        extractedPackages
        |> Array.map (fun (p,m) -> p.Name.ToLower(),m)
        |> Map.ofArray

    let applicableProjects =
        Paket.Unity3D.Unity3DReferencesFile.FindAllReferencesFiles(".")
   
    verbosefn "applicableProjects: %A" applicableProjects
        
    for unityProject in applicableProjects do    
        verbosefn "Installing to %s" unityProject.UnityAssetsDir

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

        
        verbosefn "packagesWithContent: %A" (usedPackages |> findPackagesWithContent)
           
    ()