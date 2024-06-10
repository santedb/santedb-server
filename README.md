# SanteDB iCDR Central Server

Master: ![](https://jenkins.fyfesoftware.ca/buildStatus/icon?job=public-projects/santedb-icdr-master&style=flat)
Version 3.0: [![Build Status](https://jenkins.fyfesoftware.ca/job/public-projects/job/santedb-icdr-v3/badge/icon)](https://jenkins.fyfesoftware.ca/job/public-projects/job/santedb-icdr-v3/)

## Releases 

You can get the most recent builds from our [Jenkins server](https://jenkins.fyfesoftware.ca/job/public-projects/job/santedb-icdr-v3). The most recent releases can be found in the Workspace/bin/dist folder.

## Issue Tracker & Release Tracker

If you have an issue, or would like to see our community roadmap, you can visit the [SanteDB Community](https://santesuite.atlassian.net/) JIRA project.

## Building

### Cloning the Code

SanteDB iCDR uses linked submodules in order to break the (rather large) project into more managable modules. Of course, when building the server software, you'll need all the modules. 

Obtain the code using:

```
$ git clone https://github.com/santedb/santedb-server
$ cd santedb-server
$ git submodule init
$ git submodule update --remote
```

You can set the contents of the sub-modules to a particular branch by running the ```submodule-pull BRANCH``` script, which will place all submodules into the specified BRANCH.

### Compiling on Windows

To compile on Windows you will require:

* [Microsoft Visual Studio 2019 Enterprise, Professional, or Community](https://visualstudio.microsoft.com/)
* [Inno Setup Compiler 6](https://jrsoftware.org/isdl.php)
* [7-Zip](https://www.7-zip.org/download.html)
* [Extended MSBuildTasks](https://github.com/loresoft/msbuildtasks)

There are two solution files of note on Windows:

* `santedb-server-ext.sln` -> Which contains all projects from submodules in a single solution. This is useful if you're debugging code between releases or want to use the latest submodule code 
* `santedb-server-nuget.sln` -> Which contains only projects related to the iCDR server, and references the NUGET packages. This is useful if you're just working on server components and don't want the overhead of compiling all 55 projects.

The process for compilation is as follows:

1. After cloning the solution, create a new NUGET Local repository which points to ```%localappdata%\NugetStaging```. This will be where the built nuget packages from the build process will be placed.
2. Run the `build-nuget-symbols version` command from the command line, this will build the source code in santedb-server-ext.sln file
3. Check the bin\Debug directory


### On Linux

To build on Linux you will need to install the following packages on your linux distribution (we've included package names for Ubuntu/Debian)

* Mono Project Version 6.x or higher to compile the C# code (sudo apt install mono-complete)
* (Optional) WINE Emulator to compile the Windows Installers (sudo apt install wine wine32)
* (Optional) UNZIP to compile Windows Installer (sudo apt install unzip)

You can manually build the project using msbuild:

```
./submodule-pull.sh develop
msbuild /t:clean /t:restore /t:build /p:Configuration=Debug /p:VersionNumber=`cat release-version` /p:NoFirebird=1 santedb-server-linux-ext.sln
```

If you would like to build the installers and tarballs:

```
./build-on-linux.sh VERSION_ID SOURCE_BRANCH
```

### On MacOS

To build on MacOS you will first need to install Microsoft Visual Studio for Mac or the Mono Framework 6.x or higher. 

You can build the project in debug mode with:

```
./submodule-pull.sh develop
msbuild /t:clean /t:restore /t:build /p:Configuration=Debug /p:VersionNumber=`cat release-version` /p:NoFirebird=1 santedb-server-linux-ext.sln
```
