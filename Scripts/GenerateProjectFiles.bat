@echo off
setlocal

REM Get absolute path to this script's directory
set SCRIPT_DIR=%~dp0
REM Go to project root
pushd "%SCRIPT_DIR%\.."

REM Run premake using absolute path to premake and build.lua
call "%SCRIPT_DIR%\..\ThirdParty\Binaries\Premake\premake5.exe" --file="%SCRIPT_DIR%\Build.lua" vs2022 && (
  echo Generated Project Files Successfully...
) || (
  echo Generated Project Files Failed, see error above...
)

popd
@pause