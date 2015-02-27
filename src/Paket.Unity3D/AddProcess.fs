/// Contains methods for addition of new packages
module Paket.Unity3D.AddProcess

open Paket
open System.IO
open Paket.Domain
open Paket.Unity3D

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
    
    if interactive then
        let projects = UnityProject.FindAllProjects(Path.GetDirectoryName lockFile.FileName)
        for project in projects do
            if Utils.askYesNo(sprintf "  Add to %s?" project.Name) then
                UnityProject.FindOrCreateReferencesFile(project)
                    .AddNuGetReference(package)
                    .Save()

//    if installAfter then
//        let sources = dependenciesFile.GetAllPackageSources()
//        Paket.Unity3D.InstallProcess.Install(sources, force, hard, false, lockFile)