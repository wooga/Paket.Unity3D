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
        createRelativePath r' other

[<RequireQualifiedAccessAttribute>]
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

    let ContentFiles root package =
        match ContentDir root package with
        | Some(contents) ->
            FindAllFiles(contents.FullName,"*")
            |> Array.map (fun f -> PackageFile(package,Path.Relative contents.FullName f.FullName, FileInfo(f.FullName)))
            |> Seq.ofArray
        | _ -> Seq.empty     

    let LibraryFiles package (model:Map<NormalizedPackageName,InstallModel>) =
        match model.TryFind (NormalizedPackageName package) with
        | Some(m) -> 
            m.GetLibReferences(Constants.Unity3DDotNetCompatibiliy)
            |> Seq.map (fun f -> PackageFile(package,FileInfo(f).Name,FileInfo(f)))
        | _ -> Seq.empty

    let Files root package model =
        Seq.append
        <| LibraryFiles package model
        <| ContentFiles root package

    let InstallFiles root package model (project:Project) =
        Files root package model
        |> Seq.map (fun (PackageFile(p,r,f)) -> InstallFile(PackageFile(p,r,f),Path.Combine(project.DirectorForPackage(p),r)|>FileInfo))
    
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

/// Installs all packages from the lock file.
let InstallIntoProjects(sources, options : InstallerOptions, lockFile : LockFile, projects : Paket.Unity3D.Project seq) =
    let packagesToInstall =
        projects
        |> Seq.map (fun p ->
            p.References
            |> lockFile.GetPackageHull
            |> Seq.map (fun p -> NormalizedPackageName p.Key))
        |> Seq.concat

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
        verbosefn "Installing to %s" project.Name
        
        let usedPackages = UsedPackages lockFile packages project
        
        let installFiles = 
            usedPackages 
            |> Seq.collect (fun x -> Package.InstallFiles root x.Key model project)
        
        printfn "installFiles:%A" (installFiles |> Seq.toList)
        
        do project.PaketDirectory.Create()

        let existingFiles = 
            FindAllFiles(project.PaketDirectory.FullName,"*")
        
        let delete (f:FileInfo) =
            if f.FullName.EndsWith(".meta") && installFiles |> Seq.exists (fun (Package.InstallFile(_,t)) -> f.Equals(t))
                then ()
                else do f.Delete()
        
        let install (Package.InstallFile((Package.PackageFile(p,r,f)),t)) =
            do t.Directory.Create()
            do f.CopyTo(t.FullName) |> ignore    

        existingFiles
        |> Seq.iter delete       
        
        installFiles
        |> Seq.iter install        

//        usedPackages
//        |> Seq.map (fun u -> u.Key)
//        |> Seq.map (fun p -> p,model.TryFind (NormalizedPackageName p))
//        |> Seq.choose (function | n,Some(m) -> Some(n,m) | _ -> None)
//        |> Seq.map (fun (n,m) -> n,m.GetLibReferences(Constants.Unity3DDotNetCompatibiliy))
//        |> Seq.iter (fun (n,m) -> addFilesToInstall {package=n;files=libraryFilesToInstall m} )
//        
//        usedPackages
//        |> Seq.map (fun kv -> kv.Key,findContentForPackage(root,kv.Key))
//        |> Seq.choose (function n,Some(c) -> Some(n,FindAllFiles(c.FullName,"*") |> Seq.ofArray |> contentFilesToInstall n) | _ -> None)
//        |> Seq.iter (fun (p,fs) -> addFilesToInstall {package=p;files=fs})
//        
//        do System.IO.Directory.CreateDirectory project.PaketDirectory.FullName |> ignore
//
//        let inline (+/) x y = Path.Combine(x,y)
//
//        let rec existingPackageFiles p pd (d:DirectoryInfo) =
//            seq {for f in d.GetFiles() do yield p,relativePath pd f.FullName
//                 for d' in d.GetDirectories() do yield! existingPackageFiles p pd d'}
//
//        let existingFiles =
//            seq { for d in project.PaketDirectory.GetDirectories() do 
//                    yield! existingPackageFiles (PackageName(d.Name)) (d.FullName+Path.DirectorySeparatorChar.ToString()) d}
//        
//        let willInstallFile (p:PackageName) (r:string) =
//            Map.exists (fun p' fs -> p'=p && Set.exists (function | Library(_,r) -> r=r | File(_,r) -> r=r) fs.files) !filesToInstall  
//
//        let isPlugin (s:string) =
//            Constants.PluginDirs |> Seq.exists s.StartsWith
//
//        let safeDelete (p:PackageName) (r:string) =
//            let f = (project.DirectorForPackage p) +/ r
//            if f.EndsWith(".meta") && willInstallFile p (r.Replace(".meta", "")) 
//                then () 
//                else do File.Delete(f)
//        
//        for p,f in existingFiles 
//            do safeDelete p f
//            
//        let install (p:PackageName) (f:FileToInstall) =
//            let source,target = 
//                match f with 
//                | Library(s,r) -> s,project.DirectorForPackage p +/ r
//                | File(s,r) when isPlugin r -> s,project.Assets.FullName +/ r
//                | File(s,r) -> s,project.DirectorForPackage p +/ r
//            do System.IO.Directory.CreateDirectory <| FileInfo(target).DirectoryName |> ignore
//            if File.Exists(target) 
//                then do File.Delete(target)
//            if not (FileInfo(target).Directory.Exists)
//                then do Directory.CreateDirectory(FileInfo(target).DirectoryName) |> ignore
//            do File.Copy(source,FileInfo(target).FullName)
//
//        let rec allDirs (d:DirectoryInfo) =
//            seq { for d' in d.GetDirectories() do yield! allDirs d' }
//        
//        for pfs in !filesToInstall do
//            let p = pfs.Key
//            let fs = pfs.Value
//            for f in fs.files do install p f     
//
//        for d in project.PaketDirectory.GetDirectories("*",SearchOption.AllDirectories) |> Array.rev do
//            if d.GetDirectories().Length + d.GetFiles().Length = 0 then
//                for f in d.Parent.GetFiles(d.Name+".meta")
//                    do f.Delete()
//                do d.Delete()

        ()

/// Installs all packages from the lock file.
let Install(sources, options : InstallerOptions, lockFile : LockFile) =
    let root = FileInfo(lockFile.FileName).Directory.FullName
    printfn "lockFile:%A" lockFile
    printfn "root:%A" root

    let projects = Paket.Unity3D.ReferencesFile.FindAllReferencesFiles root
                   |> Seq.map returnOrFail
    printfn "projects:%A" projects
    InstallIntoProjects(sources, options, lockFile, projects)