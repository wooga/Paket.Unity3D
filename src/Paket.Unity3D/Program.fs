/// The Executable
module Paket.Unity3D.Program

open Paket
open Paket.Logging
open System.Diagnostics
open System.Reflection
open System.IO
open System
open Nessos.UnionArgParser

let assembly = Assembly.GetExecutingAssembly()
let fvi = FileVersionInfo.GetVersionInfo(assembly.Location);

Logging.event.Publish 
|> Observable.subscribe Logging.traceToConsole
|> ignore

type Command =
    | [<First>][<CustomCommandLine("install")>] Install
    | [<First>][<CustomCommandLine("add")>]     Add
    | [<First>][<CustomCommandLine("version")>] VersionCmd
with 
    interface IArgParserTemplate with
        member __.Usage = ""
and AddArgs =
    | [<First>][<CustomCommandLine("nuget")>][<Mandatory>] Nuget of string
    | [<CustomCommandLine("version")>] Version of string
    | [<AltCommandLine("-v")>] Verbose
    | [<AltCommandLine("-f")>] Force
    | [<AltCommandLine("-i")>] Interactive
    | Hard // --hard
    | No_Install // --no-install
with 
    interface IArgParserTemplate with
        member __.Usage = ""


let commandArgs<'T when 'T :> IArgParserTemplate> args = 
    UnionArgParser.Create<'T>()
        .Parse(inputs = args, raiseOnUsage = false, ignoreMissing = true, 
               errorHandler = ProcessExiter())

let (|Command|_|) args = 
    let results = 
        UnionArgParser.Create<Command>()
            .Parse(inputs = args,
                   ignoreMissing = true, 
                   ignoreUnrecognized = true, 
                   raiseOnUsage = false)

    match results.GetAllResults() with
    | [ command ] -> Some (command, args.[1..])
    | [] -> None
    | _ -> failwith "expected only one command"

let args = Environment.GetCommandLineArgs().[1..]
try
    match args with
        | Command(Install, rest) ->
            Logging.tracefn "Paket.Unity3D version %s" fvi.FileVersion
            let results = commandArgs<AddArgs> rest 
            Paket.Logging.verbose <- results.Contains <@ AddArgs.Verbose @>
            let deps = Dependencies.Locate().DependenciesFile |> DependenciesFile.ReadFromFile
            let lock = deps.FindLockfile().FullName |> LockFile.LoadFrom
            let sources = deps.GetAllPackageSources()
            Paket.Unity3D.InstallProcess.Install(sources,InstallerOptions.Default,lock)
        | Command(Add, rest) ->
            Logging.tracefn "Paket.Unity3D version %s" fvi.FileVersion
            let results = commandArgs<AddArgs> rest
            let nuget = results.GetResult <@ AddArgs.Nuget @>
            Paket.Logging.verbose <- results.Contains <@ AddArgs.Verbose @>
            let packageName = results.GetResult <@ AddArgs.Nuget @>
            let version = defaultArg (results.TryGetResult <@ AddArgs.Version @>) ""
            let force = results.Contains <@ AddArgs.Force @>
            let hard = results.Contains <@ AddArgs.Hard @>
            let interactive = results.Contains <@ AddArgs.Interactive @>
            let noInstall = results.Contains <@ AddArgs.No_Install @>
        
            let dep = Dependencies.Locate()
            Paket.Unity3D.AddProcess.Add(dep.DependenciesFile, Paket.Domain.PackageName packageName, version, force, hard, interactive, noInstall |> not)
        | Command(VersionCmd, rest) ->
            Logging.tracefn "%s" fvi.FileVersion
        | _ -> failwith "Paket.Unity3D does not know that command"
with
| exn when not (exn :? System.NullReferenceException) -> 
    Environment.ExitCode <- 1
    traceErrorfn "Paket.Unity3D failed with:%s
    t%s" Environment.NewLine exn.Message

    if verbose then
        traceErrorfn "StackTrace:%s  %s" Environment.NewLine exn.StackTrace