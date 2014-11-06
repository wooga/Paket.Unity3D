module Paket.Unity3D.InstallProcess

open Paket
open Paket.Logging
open System.IO
open System.Collections.Generic

let private findPackagesWithContent (usedPackages:Dictionary<_,_>) = 
    usedPackages
    |> Seq.map (fun kv -> DirectoryInfo(Path.Combine("packages", kv.Key)))
    |> Seq.choose (fun packageDir -> packageDir.GetDirectories("Content") |> Array.tryFind (fun _ -> true))
    |> Seq.toList

let private copyContentFiles (project : Unity3DReferencesFile, packagesWithContent) = 

    let rules : list<(FileInfo -> bool)> = [
            fun f -> f.Name = "_._"
            fun f -> f.Name.EndsWith(".transform")
            fun f -> f.Name.EndsWith(".pp")
            fun f -> f.Name.EndsWith(".tt")
            fun f -> f.Name.EndsWith(".ttinclude")
        ]

    let onBlackList (fi : FileInfo) = rules |> List.exists (fun rule -> rule(fi))

    let rec copyDirContents (fromDir : DirectoryInfo, toDir : Lazy<DirectoryInfo>) =
        fromDir.GetDirectories() |> Array.toList
        |> List.collect (fun subDir -> copyDirContents(subDir, lazy toDir.Force().CreateSubdirectory(subDir.Name)))
        |> List.append
            (fromDir.GetFiles() 
                |> Array.toList
                |> List.filter (onBlackList >> not)
                |> List.map (fun file -> file.CopyTo(Path.Combine(toDir.Force().FullName, file.Name), true)))

    packagesWithContent
    |> List.collect (fun packageDir -> copyDirContents (packageDir, lazy (DirectoryInfo(project.UnityAssetsDir))))

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
        |> Seq.iter (fun (path,dlls) -> 
                                        Utils.CreateDir path
                                        for dll in dlls do dll.CopyTo(Path.Combine(path, dll.Name)) |> ignore )

        verbosefn "usedPackages: %A" usedPackages

        //project.UpdateReferences(model,usedPackages,hard)
        
        //removeCopiedFiles project

        //let getGitHubFilePath name = 
        //    (lockFile.SourceFiles |> List.find (fun f -> Path.GetFileName(f.Name) = name)).FilePath

//        let gitHubFileItems =
//            referenceFile.GitHubFiles
//            |> List.map (fun file -> 
//                             { BuildAction = project.DetermineBuildAction file.Name 
//                               Include = createRelativePath project.FileName (getGitHubFilePath file.Name)
//                               Link = Some(if file.Link = "." then Path.GetFileName(file.Name)
//                                           else Path.Combine(file.Link, Path.GetFileName(file.Name))) })
//        
        let nuGetFileItems =
            if lockFile.Options.OmitContent then [] else
            copyContentFiles(unityProject, findPackagesWithContent usedPackages)
        
        ()
            
//            files |> List.map (fun file -> 
//                                    { BuildAction = project.DetermineBuildAction file.Name
//                                      Include = createRelativePath project.FileName file.FullName
//                                      Link = None })

//        project.UpdateFileItems(gitHubFileItems @ nuGetFileItems, hard)

//        project.Save() 
    
    ()