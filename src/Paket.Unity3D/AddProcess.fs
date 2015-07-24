/// Contains methods for addition of new packages
module Paket.Unity3D.AddProcess

open Paket
open System.IO
open Paket.Unity3D

let private add (project:DirectoryInfo) package =
        UnityProject.FindOrCreateReferencesFile(project)
            .AddNuGetReference(package)
            .Save()    

let Add(dependenciesFileName, package, version, force, hard, interactive, installAfter) =
    let existingDependenciesFile = DependenciesFile.ReadFromFile(dependenciesFileName)

    let dependenciesFile =
        existingDependenciesFile
          .Add(package,version)
    
    if existingDependenciesFile.Packages.IsEmpty then
        Paket.Logging.traceWarn "Dependencies file did not contain packages; source might have been replaced or removed!"    

    dependenciesFile.Save()

    let lockFile = dependenciesFile.FindLockfile().FullName |> LockFile.LoadFrom

    //let lockFile = UpdateProcess.SelectiveUpdate(dependenciesFile,Some(NormalizedPackageName package),force)
    
    let projects = UnityProject.FindAllProjects(Path.GetDirectoryName lockFile.FileName)

    if interactive then
        for project in projects do
            if Utils.askYesNo(sprintf "  Add package to UnityProject: %s at %s?" project.Name project.Parent.FullName) then
                add project package
    else if projects.Length=1 then
        add projects.[0] package
    else if projects.Length > 1 then
        Paket.Logging.traceWarn "More than one unity project was found, please run interactive mode (--interactive) to pick and choose target project(s)"
            

//    if installAfter then
//        let sources = dependenciesFile.GetAllPackageSources()
//        Paket.Unity3D.InstallProcess.Install(sources, force, hard, false, lockFile)