## Build

### Gossamer.GfxCompiler
```PowerShell
dotnet build Gossamer.GfxCompiler/Gossamer.GfxCompiler.csproj /p:Configuration=Release /p:Platform=x64
```

## Dependencies

External dependencies for Windows are built using MSVC x64 v143 toolset. Ensure the latest [MSVC redistributable](https://aka.ms/vs/17/release/vc_redist.x64.exe) is installed on a target machine without Visual Studio installed (possible end users).
Failure to have the redistributable installed will result in a missing DLL error or a crash when running the application.