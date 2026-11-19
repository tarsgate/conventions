@ECHO OFF
where /q dotnet
IF ERRORLEVEL 1 (
    ECHO "ERROR: 'dotnet' not found. Please ensure you have installed .NET (the version specified in global.json)" && EXIT /b 1
) ELSE (
    IF "%~1" == "format" (
        npm run format
        dotnet dnx --yes fantomless-tool --recurse .
    ) ELSE (
        ECHO "Target was not recognized" && EXIT /b 1
    )
)
