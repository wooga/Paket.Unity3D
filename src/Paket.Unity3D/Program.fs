/// The Executable
module Paket.Unity3D.Program

open Paket
Paket.Logging.verbose <- true

let lockFileName = DependenciesFile.FindLockfile Constants.DependenciesFile
let lockFile = LockFile.LoadFrom lockFileName.FullName
let dependencies = DependenciesFile.ReadFromFile Constants.DependenciesFile
Paket.Unity3D.InstallProcess.Install(dependencies.Sources, false, false, lockFile)