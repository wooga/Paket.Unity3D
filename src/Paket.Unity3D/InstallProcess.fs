/// Contains methods for the install process.
module Paket.Unity3D.InstallProcess

open Paket
open Chessie.ErrorHandling
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
open Paket.Requirements
open System.Security.AccessControl

let findPackageFolder root (PackageName name) =
    let lowerName = name.ToLower()
    let di = DirectoryInfo(Path.Combine(root, Constants.PackagesFolderName))
    let direct = DirectoryInfo(Path.Combine(di.FullName, name))
    if direct.Exists then direct else
    match di.GetDirectories() |> Seq.tryFind (fun subDir -> subDir.FullName.ToLower().EndsWith(lowerName)) with
    | Some x -> x
    | None -> failwithf "Package directory for package %s was not found." name

let private findContentForPackage (root,package:PackageName) =
    let folder = findPackageFolder root package
    folder.GetDirectories("Content")
    |> Array.append (folder.GetDirectories("content"))
    |> Array.tryFind (fun _ -> true)

let private findPackagesWithContent (root,usedPackages:Map<PackageName,InstallSettings>) =
    usedPackages
    |> Seq.filter (fun kv -> defaultArg kv.Value.OmitContent false |> not)
    |> Seq.choose (fun kv -> findContentForPackage(root,kv.Key))
    |> Seq.toList

let rec filesInDir (d:DirectoryInfo) =
    seq{ for f in d.GetFiles() do yield f.FullName
         for d' in d.GetDirectories() do yield! filesInDir d' }

let private copyContentFiles (project : ProjectFile, packagesWithContent) =

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
    |> List.collect (fun packageDir -> copyDirContents (packageDir, lazy (DirectoryInfo(Path.GetDirectoryName(project.FileName)))))

let private removeCopiedFiles (project: ProjectFile) =
    let rec removeEmptyDirHierarchy (dir : DirectoryInfo) =
        if dir.Exists && dir.EnumerateFileSystemInfos() |> Seq.isEmpty then
            dir.Delete()
            removeEmptyDirHierarchy dir.Parent

    let removeFilesAndTrimDirs (files: FileInfo list) =
        for f in files do
            if f.Exists then
                f.Delete()

        let dirsPathsDeepestFirst =
            files
            |> Seq.map (fun f -> f.Directory.FullName)
            |> Seq.distinct
            |> List.ofSeq
            |> List.rev

        for dirPath in dirsPathsDeepestFirst do
            removeEmptyDirHierarchy (DirectoryInfo dirPath)

    project.GetPaketFileItems()
    |> List.filter (fun fi -> not <| fi.FullName.Contains(Constants.PaketFilesFolderName))
    |> removeFilesAndTrimDirs

let CreateInstallModel(root, sources, force, package) =
    async {
        let! (package, files, targetsFiles) = RestoreProcess.ExtractPackage(root, sources, force, package)
        let (PackageName name) = package.Name
        let nuspec = Nuspec.Load(root,package.Name)
        let files = files |> Array.map (fun fi -> fi.FullName)
        let targetsFiles = targetsFiles |> Array.map (fun fi -> fi.FullName)
        return package,InstallModel.CreateFromLibs(package.Name, package.Version, package.Settings.FrameworkRestrictions, files, targetsFiles, nuspec)
        }

/// Restores the given packages from the lock file.
let createModel(root, sources, force, lockFile : LockFile, packages:Set<NormalizedPackageName>) =
    let sourceFileDownloads = RemoteDownload.DownloadSourceFiles(root, force, lockFile.SourceFiles)

    let packageDownloads =
        lockFile.ResolvedPackages
        |> Map.filter (fun name _ -> packages.Contains name)
        |> Seq.map (fun kv -> CreateInstallModel(root,sources,force,kv.Value))
        |> Async.Parallel

    let _,extractedPackages =
        Async.Parallel(sourceFileDownloads,packageDownloads)
        |> Async.RunSynchronously

    extractedPackages

//let findAllReferencesFiles root =
//    root
//    |> ProjectFile.FindAllProjects
//    |> Array.choose (fun p -> ProjectFile.FindReferencesFile(FileInfo(p.FileName))
//                                |> Option.map (fun r -> p, r))
//    |> Array.map (fun (project,file) ->
//        try
//            ok <| (project, ReferencesFile.FromFile(file))
//        with _ ->
//            fail <| ReferencesFileParseError (FileInfo(file)))
//    |> collect

/// Installs all packages from the lock file.
let InstallIntoProjects(sources, options : InstallerOptions, lockFile : LockFile, projects : Paket.Unity3D.Project seq) =
    let packagesToInstall =
        if options.OnlyReferenced then
            projects
//            |> Seq.ofList
            |> Seq.map (fun p ->
                p.References
                |> lockFile.GetPackageHull
                |> Seq.map (fun p -> NormalizedPackageName p.Key))
            |> Seq.concat
        else
            lockFile.ResolvedPackages
            |> Seq.map (fun kv -> kv.Key)

    let root = Path.GetDirectoryName lockFile.FileName
    let extractedPackages = createModel(root, sources, options.Force, lockFile, Set.ofSeq packagesToInstall)
    let lookup = lockFile.GetDependencyLookupTable()

    let model =
        extractedPackages
        |> Array.map (fun (p,m) -> NormalizedPackageName p.Name,m)
        |> Map.ofArray

    let packages =
        extractedPackages
        |> Array.map (fun (p,m) -> NormalizedPackageName p.Name,p)
        |> Map.ofArray

    for project in projects do
        verbosefn "Installing to %s" project.Name
        
        let usedPackages =
            project.References.NugetPackages
            |> Seq.map (fun ps ->
                let package = 
                    match packages |> Map.tryFind (NormalizedPackageName ps.Name) with
                    | Some p -> p
                    | None -> failwithf "%s uses NuGet package %O, but it was not found in the paket.lock file." project.References.FileName ps.Name

                let resolvedSettings = [lockFile.Options.Settings; package.Settings] |> List.fold (+) ps.Settings
                ps.Name, resolvedSettings
                )
            |> Map.ofSeq

        let usedPackages =
            let d = ref usedPackages

            /// we want to treat the settings from the references file through the computation so that it can be used as the base that 
            /// the other settings modify.  in this way we ensure that references files can override the dependencies file, which in turn overrides the lockfile.
            let usedPackageDependencies = 
                usedPackages 
                |> Seq.collect (fun u -> lookup.[NormalizedPackageName u.Key] |> Seq.map (fun i -> u.Value, i))
                |> Seq.choose (fun (parentSettings, dep) -> 
                    match packages |> Map.tryFind (NormalizedPackageName dep) with
                    | None -> None
                    | Some p -> 
                        let resolvedSettings = [lockFile.Options.Settings; p.Settings] |> List.fold (+) parentSettings
                        Some (p.Name, resolvedSettings) )

            for name,settings in usedPackageDependencies do
                if (!d).ContainsKey name |> not then
                  d := Map.add name settings !d

            !d

        let usedPackageSettings =
            usedPackages
            |> Seq.map (fun u -> NormalizedPackageName u.Key,u.Value)
            |> Map.ofSeq

        let libs = 
            usedPackageSettings
            |> Seq.map (fun u -> u.Key)
            |> Seq.map (fun p -> p,model.TryFind p)
            |> Seq.choose (function | n,Some(m) -> Some(n,m) | _ -> None)
            |> Seq.map (fun (n,m) -> n,m.GetLibReferences(FrameworkIdentifier.DotNetFramework(FrameworkVersion.V3_5)))
                
        printfn "libs:%A" libs

        let contents =
            usedPackages
            |> Seq.map (fun kv -> NormalizedPackageName kv.Key,findContentForPackage(root,kv.Key))
            |> Seq.choose (function n,Some(c) -> Some(n,filesInDir c) | _ -> None)
             
        printfn "contents:%A" contents

        ()

//        project.UpdateReferences(model, usedPackageSettings, options.Hard)


//        let gitRemoteItems =
//            project.References.RemoteFiles
//            |> List.map (fun file ->
//                             let link = if file.Link = "." then Path.GetFileName file.Name else Path.Combine(file.Link, Path.GetFileName file.Name)
//                             let remoteFilePath = 
//                                if verbose then
//                                    tracefn "FileName: %s " file.Name 
//
//                                match lockFile.SourceFiles |> List.tryFind (fun f -> Path.GetFileName(f.Name) = file.Name) with
//                                | Some file -> file.FilePath root
//                                | None -> failwithf "%s references file %s, but it was not found in the paket.lock file." project.References.FileName file.Name
//
//                             let linked = defaultArg file.Settings.Link true
//
//                             if linked then
//                                 { BuildAction = project.DetermineBuildAction file.Name
//                                   Include = createRelativePath project.FileName remoteFilePath
//                                   Link = Some link }
//                             else
//                                 let toDir = Path.GetDirectoryName(project.FileName)
//                                 let targetFile = FileInfo(Path.Combine(toDir,link))
//                                 if targetFile.Directory.Exists |> not then
//                                    targetFile.Directory.Create()
//
//                                 File.Copy(remoteFilePath,targetFile.FullName)
//
//                                 { BuildAction = project.DetermineBuildAction file.Name
//                                   Include = createRelativePath project.FileName targetFile.FullName
//                                   Link = None })

//        let nuGetFileItems =
//            copyContentFiles(project, findPackagesWithContent(root,usedPackages))
//            |> List.map (fun file ->
//                                { BuildAction = project.DetermineBuildAction file.Name
//                                  Include = createRelativePath project.FileName file.FullName
//                                  Link = None })

//        project.UpdateFileItems(gitRemoteItems @ nuGetFileItems, options.Hard)
//
//        project.Save()

//    if options.Redirects || lockFile.Options.Redirects then
//        applyBindingRedirects root extractedPackages

/// Installs all packages from the lock file.
let Install(sources, options : InstallerOptions, lockFile : LockFile) =
    let root = FileInfo(lockFile.FileName).Directory.FullName
    printfn "lockFile:%A" lockFile
    printfn "root:%A" root

    let projects = Paket.Unity3D.ReferencesFile.FindAllReferencesFiles root
                   |> Seq.map returnOrFail
    printfn "projects:%A" projects
    InstallIntoProjects(sources, options, lockFile, projects)