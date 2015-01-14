module Paket.Unity3D.InstallProcess

open Paket
open Paket.Rop
open Paket.Domain
open Paket.Logging
open Paket.BindingRedirects
open Paket.ModuleResolver
open Paket.PackageResolver
open System.IO
open System.Collections.Generic
open FSharp.Polyfill
open System.Reflection
open System.Diagnostics

let private findPackagesWithContent (root,usedPackages:HashSet<_>) = 
    usedPackages
    |> Seq.map (fun (PackageName x) -> x,DirectoryInfo(Path.Combine(root, Constants.PackagesFolderName, x)))
    |> Seq.choose (fun (name,packageDir) -> if packageDir.GetDirectories("content").Length>0
                                            then Some(name,(DirectoryInfo(Path.Combine(packageDir.FullName,"content"))))
                                            else None)
    |> Seq.toList

let private copyContentFiles (project : Paket.Unity3D.Project, packagesWithContent:(string*DirectoryInfo) list) = 

    let rules : list<(FileInfo -> bool)> = [
            fun f -> f.Name = "_._"
            fun f -> f.Name.EndsWith(".transform")
            fun f -> f.Name.EndsWith(".pp")
            fun f -> f.Name.EndsWith(".tt")
            fun f -> f.Name.EndsWith(".ttinclude")
        ]

    let onBlackList (fi : FileInfo) = rules |> List.exists (fun rule -> rule(fi))

    let copyFiles (fromDir:DirectoryInfo) (toDir:Lazy<DirectoryInfo>) :FileSystemInfo list =
        fromDir.GetFiles() 
        |> Array.toList
        |> List.filter (onBlackList >> not)
        |> List.map (fun file -> file.CopyTo(Path.Combine(toDir.Force().FullName, file.Name), true) :> FileSystemInfo)

    let rec copyDirContents (package:string, fromDir : DirectoryInfo, toDir : Lazy<DirectoryInfo>) :FileSystemInfo list =
        fromDir.GetDirectories() |> Array.toList
        |> List.collect (fun subDir -> copyDirContents(package, subDir, lazy toDir.Force().CreateSubdirectory(subDir.Name)))
        |> List.append (copyFiles fromDir toDir)
            
    let copyContentDirContents (package:string, fromDir : DirectoryInfo) =
        tracefn "- %s" package
        let lazyDir dir = lazy(
            let info = DirectoryInfo(dir) in do info.Create()
            info
            )
        let targetDir = lazyDir(Path.Combine(project.PaketDirectory.FullName, package))
        fromDir.GetDirectories() |> Array.toList
        |> List.map (fun subDir -> (subDir,match subDir.Name with "Plugins" -> lazy(project.Assets) | _ -> targetDir))
        |> List.collect (fun (subDir,target) -> copyDirContents(package, subDir, lazy target.Force().CreateSubdirectory(subDir.Name)))
        |> List.append (copyFiles fromDir targetDir)

    packagesWithContent
    |> List.collect (fun (name,packageDir) -> copyContentDirContents (name, packageDir))

let private removeCopiedFiles (project:Paket.Unity3D.Project) =
    if project.PaketDirectory.Exists then project.PaketDirectory.Delete(true)

let CreateInstallModel(root, sources, force, package) = 
    async { 
        let! (package, files) = RestoreProcess.ExtractPackage(root, sources, force, package)
        let (PackageName name) = package.Name
        let nuspec = FileInfo(sprintf "%s/packages/%s/%s.nuspec" root name name)
        let nuspec = Nuspec.Load nuspec.FullName
        let files = files |> Seq.map (fun fi -> fi.FullName)
        return package, InstallModel.CreateFromLibs(package.Name, package.Version, package.FrameworkRestrictions, files, nuspec)
    }

/// Restores the given packages from the lock file.
let createModel(root, sources,force, lockFile:LockFile) = 
    let sourceFileDownloads = RemoteDownload.DownloadSourceFiles(root, lockFile.SourceFiles)
        
    let packageDownloads = 
        lockFile.ResolvedPackages
        |> Seq.map (fun kv -> CreateInstallModel(root,sources,force,kv.Value))
        |> Async.Parallel

    let _,extractedPackages =
        Async.Parallel(sourceFileDownloads,packageDownloads)
        |> Async.RunSynchronously

    extractedPackages

let findAllReferencesFiles root =
    root |> Paket.Unity3D.ReferencesFile.FindAllReferencesFiles |> collect

/// Installs the given all packages from the lock file.
let InstallIntoProjects(sources,force, hard, withBindingRedirects, lockFile:LockFile, projects:Paket.Unity3D.Project list) =
    let root = Path.GetDirectoryName lockFile.FileName
    let extractedPackages = createModel(root,sources,force, lockFile)

    let model =
        extractedPackages
        |> Array.map (fun (p,m) -> NormalizedPackageName p.Name,m)
        |> Map.ofArray

    for project in projects do    
        tracefn "Installing to %s" project.Name

        let usedPackages = lockFile.GetPackageHull(project.References)

        let usedPackageNames =
            usedPackages
            |> Seq.map NormalizedPackageName
            |> Set.ofSeq

        removeCopiedFiles project

        copyContentFiles(project, findPackagesWithContent(root,usedPackages))
        |> ignore

/// Installs the given all packages from the lock file.
let Install(sources,force, hard, withBindingRedirects, lockFile:LockFile) = 
    let root = FileInfo(lockFile.FileName).Directory.FullName 
    let projects = findAllReferencesFiles root |> returnOrFail
    InstallIntoProjects(sources,force,hard,withBindingRedirects,lockFile,projects)