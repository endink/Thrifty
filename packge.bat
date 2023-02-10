set PROJ_DIR=%~dp0src
set OUT=%~dp0nupkgs
dotnet pack %PROJ_DIR%\Thrifty.Core\Thrifty.Core.csproj --output %OUT% -c Release 
dotnet pack %PROJ_DIR%\Thrifty.MicroServices\Thrifty.MicroServices.csproj --output %OUT% -c Release
dotnet pack %PROJ_DIR%\Thrifty.Nifty\Thrifty.Nifty.csproj --output %OUT% -c Release
dotnet pack %PROJ_DIR%\Thrifty.Nifty.Client\Thrifty.Nifty.Client.csproj --output %OUT% -c Release
dotnet pack %PROJ_DIR%\Thrifty.Services\Thrifty.Services.csproj --output %OUT% -c Release


@echo off  
setlocal enabledelayedexpansion  
  
echo ɾ�����ţ�
for /R %OUT% %%f in (*.symbols.nupkg) do ( 
del /f /q %%f
)


echo Ҫ�����İ���
for /R %OUT% %%f in (*.nupkg) do ( 
echo publish %%f
)


set /p input=ȷ�ϰ� y�� ȡ�����������

if /i not "%input%"=="y" goto exit

for /R %dir% %%f in (*.nupkg) do ( 
echo ��ʼ�ϴ� %%f
dotnet nuget push %%f -s https://api.nuget.org/v3/index.json
)

pause