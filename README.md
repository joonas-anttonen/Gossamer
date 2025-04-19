# Gossamer

## Repository

When cloning the repository, use the `--recurse-submodules` flag to ensure all submodules are cloned as well.
Alternatively, run `git submodule update --init --recursive` after cloning.

## Build

Building this project from scratch is a 3-step process: build the shader compiler, build the shaders, and build the project.\
First step is only required if changes have been made to the shader compiler.\
Second step is only required if changes have been made to the shaders.

All steps are implemented as tasks in vscode but command line work just as well, even faster.

### Build the shader compiler

```PowerShell
dotnet build Gossamer.GfxCompiler/Gossamer.GfxCompiler.csproj /p:Configuration=Release /p:Platform=x64
```

### Compile the built-in shaders

Requires the shader compiler to be built.

> Linux: By default, dxc doesn't have execution permission and can't find libdxcompiler when executed.\
> Run `chmod +x Gossamer.GfxCompiler/bin/x64/Release/External/dxc && export LD_LIBRARY_PATH=$LD_LIBRARY_PATH:$(readlink --canonicalize Gossamer.GfxCompiler/bin/x64/Release/External)` for a quick fix.

Task `Compile Shaders` or

```PowerShell
Gossamer.GfxCompiler/bin/x64/Release/Gossamer.GfxCompiler ../../../../Gossamer/Backend/Shaders ../../../../Gossamer/Backend/Shaders/built-in.shaders
```

### Build the project

Requires the compiled shaders.

Task `Build` or

```PowerShell
dotnet build Gossamer.sln /p:Configuration=Debug /p:Platform=x64
```

## About dependencies

External dependencies for Windows are built using MSVC x64 v143 toolset. Ensure the latest [MSVC redistributable](https://aka.ms/vs/17/release/vc_redist.x64.exe) is installed on a target machine without Visual Studio installed (possible end users).
Failure to have the redistributable installed will result in a missing DLL error or a crash when running the application.
