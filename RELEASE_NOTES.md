#### 0.2.2 - July 24 2015
* Enables trace-logs and adds --verbose argument

#### 0.2.1 - July 24 2015
* Removes debug logs

#### 0.2.0 - July 24 2015
* Supports DLL packages (uses `net35` for Unity3D projects)
* Allows for omit-content and framework restrictions in paket.references file
* Does not delete .meta files unless they are really removed

#### 0.1.0 - April 14 2015
* Adds `PAKET.UNITY3D.VERSION` environment veriable to request a specific version via the paket.unity3d.bootstrapper.exe

#### 0.0.14 - February 26 2015
* Add command installs automatically when single project is found and displays a warning when multiple projects are found but interactive command is not given

#### 0.0.12 - February 26 2015
* Implements Add commands

#### 0.0.11 - February 26 2015
* Removes debug information from bootstrapper

#### 0.0.10 - February 25 2015
* Git merge hickup has been fixed and paket.unity3d.exe should be working again

#### 0.0.9 - February 25 2015
* Attaches AssemblyInfo version numbers so the bootstrapper can function

#### 0.0.8 - February 25 2015
* Adds Paket.Unity3D.Bootstrapper

#### 0.0.7 - January 19 2015
* More documentation and guides
* Example project

#### 0.0.6 - January 14 2015
* Updates for Paket.Core 0.22.9
* Does not modify paths of "Plugins" directories
* Removed DLL embedding (postponed until Unity5 where you can link DLLs per platform)

#### 0.0.5 - November 12 2014
* Cleans `Paket.Unity3D` directories in Unity3D projects when updating dependencies

#### 0.0.4 - November 8 2014  
* Not a prerelease anymore

#### 0.0.3-beta - November 7 2014  
* Uses ILRepack to merge exe with dlls

#### 0.0.2-beta - November 7 2014  
* Bundles dll files along in tools directory

#### 0.0.1-beta - November 7 2014  
* Installs libraries and content files from nuget-packages, respects Plugin directories
* Initial beta-release
