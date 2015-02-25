/// The Executable
module Paket.Unity3D.Program

open Paket
open Paket.Logging
open System.Diagnostics
open System.Reflection
open System.IO
open System

let assembly = Assembly.GetExecutingAssembly()
let fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
tracefn "Paket.Unity3D version %s" fvi.FileVersion

try 
    let deps = Dependencies.Locate().DependenciesFile |> DependenciesFile.ReadFromFile
    let lock = deps.FindLockfile().FullName |> LockFile.LoadFrom
    let sources =deps.GetAllPackageSources()
    Paket.Unity3D.InstallProcess.Install(sources,false,true,false,lock)
with
| exn when not (exn :? System.NullReferenceException) -> 
    Environment.ExitCode <- 1
    traceErrorfn "Paket failed with:%s\t%s" Environment.NewLine exn.Message

    if verbose then
        traceErrorfn "StackTrace:%s  %s" Environment.NewLine exn.StackTrace