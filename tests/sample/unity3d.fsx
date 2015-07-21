//#I "../../"
#I "../../packages/Paket.Core/lib/net45"
#I "../../packages/Newtonsoft.Json/lib/net45"
#r "Paket.Core"
#load "../../src/Paket.Unity3D/Constants.fs"
#load "../../src/Paket.Unity3D/ReferencesFile.fs"
#load "../../src/Paket.Unity3D/InstallProcess.fs"

open Paket

let DIR = __SOURCE_DIRECTORY__

System.Environment.CurrentDirectory <- DIR

Paket.Logging.verbose <- true

module Commands =
    let Install () =
        let deps = Dependencies.Locate().DependenciesFile |> DependenciesFile.ReadFromFile
        let lock = deps.FindLockfile().FullName |> LockFile.LoadFrom
        let sources = deps.GetAllPackageSources()
        Paket.Unity3D.InstallProcess.Install(sources,InstallerOptions.Default,lock)