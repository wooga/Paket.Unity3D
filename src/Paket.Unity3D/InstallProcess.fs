/// Contains methods for the install process.
module Paket.Unity3D.InstallProcess

open Paket
open Chessie.ErrorHandling
open Paket.Domain
open Paket.Logging
open Paket.PackageResolver
open System.IO
open FSharp.Polyfill
open Paket.Requirements

module Path =
    let Relative (root:string) other =
        let sep = Path.DirectorySeparatorChar.ToString()
        let r' = if root.EndsWith(sep) then root else root + sep
        (createRelativePath r' other).Replace('\\',Path.DirectorySeparatorChar)

[<RequireQualifiedAccess>]
module private Package =

    [<StructuredFormatDisplay("PackageFile({package},{relative},{file})")>]
    type PackageFile = PackageFile of package:PackageName * relative:string * file:FileInfo
    [<StructuredFormatDisplay("InstallFile({source},{target})")>]
    type InstallFile = InstallFile of source:PackageFile * target:FileInfo

    let Dir root (PackageName name) =
        let lowerName = name.ToLower()
        let di = DirectoryInfo(Path.Combine(root, Constants.PackagesFolderName))
        let direct = DirectoryInfo(Path.Combine(di.FullName, name))
        if direct.Exists then direct else
        match di.GetDirectories() |> Seq.tryFind (fun subDir -> subDir.FullName.ToLower().EndsWith(lowerName)) with
        | Some x -> x
        | None -> failwithf "Package directory for package %s was not found." name

    let ContentDir root package =
        let folder = Dir root package
        folder.GetDirectories("Content")
        |> Array.append (folder.GetDirectories("content"))
        |> Array.tryFind (fun _ -> true)

    let ContentFiles root package (settings:PackageInstallSettings option) =
        printfn "settings:%A" settings 
        match settings with
        | Some(s) when s.Settings.OmitContent.IsSome && s.Settings.OmitContent.Value -> Seq.empty
        | _ -> 
            match ContentDir root package with
            | Some(contents) ->
                FindAllFiles(contents.FullName,"*")
                |> Array.map (fun f -> PackageFile(package,Path.Relative contents.FullName f.FullName, FileInfo(f.FullName)))
                |> Seq.ofArray
            | _ -> Seq.empty     

    let LibraryFiles package (model:Map<NormalizedPackageName,InstallModel>) (settings:PackageInstallSettings option) =
        let fwrs =
            match settings with
            | Some(s) -> s.Settings.FrameworkRestrictions
            | _ -> Constants.Unity3DFrameworkRestrictions
        
        match model.TryFind (NormalizedPackageName package) with
        | Some(m) -> 
            m.ApplyFrameworkRestrictions(fwrs)
             .GetLibReferences(Constants.Unity3DDotNetCompatibiliy)
            |> Seq.map (fun f -> PackageFile(package,FileInfo(f).Name,FileInfo(f)))
        | _ -> Seq.empty

    let Files root package (settings:PackageInstallSettings option) model =
        Seq.append
        <| LibraryFiles package model settings
        <| ContentFiles root package settings

    let InstallFiles root (settings:Map<NormalizedPackageName,PackageInstallSettings>) package model (project:Project) =
        Files root package (settings.TryFind (NormalizedPackageName package)) model
        |> Seq.map (fun p ->
            
            let t = 
                match p with
                | PackageFile(_,r,_) when r.StartsWith("Plugins") -> 
                    let pp = Path.Combine(project.Assets.FullName,r)
                    printfn "plugins:%A" pp
                    pp
                | PackageFile(p,r,_) -> Path.Combine(project.DirectorForPackage(p),r)
                |>FileInfo
            InstallFile(p,t)
            )
    
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

let UsedPackages (lockFile:LockFile) (packages:Map<NormalizedPackageName,ResolvedPackage>) (project:Project) =
    let lookup = lockFile.GetDependencyLookupTable()
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
    usedPackages    

type MetaFile =
    | FileMetaFile of file:FileInfo * metaFile:FileInfo
    | DirectoryMetaFile of dir:DirectoryInfo * metaFile:FileInfo
    | DeadMetafile of metaFile:FileInfo
    | NotAMetaFile of file:FileInfo
    with 
        static member Of(f:FileInfo) = 
            match f.FullName.Replace(".meta","") with
            | _ when not <| f.FullName.EndsWith(".meta") -> NotAMetaFile f
            | t when File.Exists(t) -> FileMetaFile(FileInfo t,f)
            | t when Directory.Exists(t) -> DirectoryMetaFile(DirectoryInfo t,f)
            | _ -> DeadMetafile f

/// Installs all packages from the lock file.
let InstallIntoProjects(sources, options : InstallerOptions, lockFile : LockFile, projects : Paket.Unity3D.Project seq) =
    let packagesToInstall =
        projects
        |> Seq.map (fun proj ->
            proj.References
            |> lockFile.GetPackageHull
            |> Seq.map (fun p -> NormalizedPackageName p.Key))
        |> Seq.concat
    
    let settings =
        projects
        |> Seq.map (fun proj ->
            proj.References
            |> lockFile.GetPackageHull
            |> Seq.map (fun p -> NormalizedPackageName p.Key,p.Value))
        |> Seq.concat
        |> Map.ofSeq

    let root = Path.GetDirectoryName lockFile.FileName
    let extractedPackages = createModel(root, sources, options.Force, lockFile, Set.ofSeq packagesToInstall)
    
    let model =
        extractedPackages
        |> Array.map (fun (p,m) -> NormalizedPackageName p.Name,m)
        |> Map.ofArray

    let packages =
        extractedPackages
        |> Array.map (fun (p,_) -> NormalizedPackageName p.Name,p)
        |> Map.ofArray

    for project in projects do
        printfn "Installing to %s" project.Name
        
        let usedPackages = UsedPackages lockFile packages project
        
        usedPackages
        |> Seq.iter (fun p -> let (PackageName n) = p.Key in printfn "- %s" n)

        let installFiles = 
            usedPackages 
            |> Seq.collect (fun x -> Package.InstallFiles root settings x.Key model project)
        
        do project.PaketDirectory.Create()

        let removeNonMetaFiles d =
            FindAllFiles(d,"*")
            |> Seq.map MetaFile.Of
            |> Seq.iter (function NotAMetaFile f -> f.Delete() | _ -> ())

        let removeDeadMetaFiles d =
            FindAllFiles(d,"*.meta")
            |> Seq.map MetaFile.Of
            |> Seq.iter (function DeadMetafile f -> f.Delete() | _ -> ())

        let rec removeDeadDirs (d:DirectoryInfo) =
            for d' in d.GetDirectories() do removeDeadDirs d'
            do removeDeadMetaFiles d.FullName
            if d.EnumerateFileSystemInfos() |> Seq.isEmpty then d.Delete()    

        let install (Package.InstallFile((Package.PackageFile(p,r,f)),t)) =
            do t.Directory.Create()
            if t.Exists then do t.Delete()
            do f.CopyTo(t.FullName) |> ignore    

        do removeNonMetaFiles project.PaketDirectory.FullName
        
        installFiles
        |> Seq.iter install

        do removeDeadDirs project.PaketDirectory

/// Installs all packages from the lock file.
let Install(sources, options : InstallerOptions, lockFile : LockFile) =
    let root = FileInfo(lockFile.FileName).Directory.FullName
    let projects = Paket.Unity3D.ReferencesFile.FindAllReferencesFiles root
                   |> Seq.map returnOrFail
    InstallIntoProjects(sources, options, lockFile, projects)