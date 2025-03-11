# Gossamer

## Build

### Build the shader compiler

```PowerShell
dotnet build Gossamer.GfxCompiler/Gossamer.GfxCompiler.csproj /p:Configuration=Release /p:Platform=x64
```

### Compile built-in shaders

Depends on the shader compiler.

Task `Compile Shaders` or

```PowerShell
Gossamer.GfxCompiler/bin/x64/Release/Gossamer.GfxCompiler ../../../../Gossamer/Backend/Shaders ../../../../Gossamer/Backend/Shaders/built-in.shaders
```

### Build the project

Depends on the compiled shaders.

Task `Build` or

```PowerShell
dotnet build Gossamer.sln /p:Configuration=Debug /p:Platform=x64
```

## About dependencies

External dependencies for Windows are built using MSVC x64 v143 toolset. Ensure the latest [MSVC redistributable](https://aka.ms/vs/17/release/vc_redist.x64.exe) is installed on a target machine without Visual Studio installed (possible end users).
Failure to have the redistributable installed will result in a missing DLL error or a crash when running the application.
