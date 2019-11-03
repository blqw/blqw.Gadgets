@echo off 
echo "■■■■■■■■■■■■■■■■■■■■■■ 变量是: %1 ■■■■■■■■■■■■■■■■■■■■■■"


del %CD%\unpkgs\*.nupkg
if "%1"=="" (
	dotnet build -c Debug
) else (
	dotnet build -c Release
)
echo "■■■■■■■■■■■■■■■■■■■■■■ 编译完成 ■■■■■■■■■■■■■■■■■■■■■■"

if "%1"=="" (
	dotnet pack -c Debug -o %CD%\unpkgs
) else (
	dotnet pack -c Release -o %CD%\unpkgs
)
echo "■■■■■■■■■■■■■■■■■■■■■■ 打包完成 ■■■■■■■■■■■■■■■■■■■■■■"
for /r . %%a in (unpkgs\*.nupkg) do (
    if "%1"=="" (
        md "d:\appdata\nuget_local\"
		echo "■■■■■■■■■■■■■■■■■■■■■■ 安装到本地 ■■■■■■■■■■■■■■■■■■■■■■"
        dotnet nuget push "%%a" -s "d:\appdata\nuget_local"
    ) else (
        dotnet nuget push "%%a" -s https://api.nuget.org/v3/index.json -k %1%
    )
)
echo "■■■■■■■■■■■■■■■■■■■■■■ 安装完成 ■■■■■■■■■■■■■■■■■■■■■■"
del %CD%\unpkgs\*.nupkg