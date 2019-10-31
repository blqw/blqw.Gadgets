@echo off 
echo "■■■■■■■■■■■■■■■■■■■■■■ 变量是: %1 ■■■■■■■■■■■■■■■■■■■■■■"

del %CD%\unpkgs\*.nupkg
dotnet build -c Release
echo "■■■■■■■■■■■■■■■■■■■■■■ 编译完成 ■■■■■■■■■■■■■■■■■■■■■■"
dotnet pack --no-build -o %CD%\unpkgs
echo "■■■■■■■■■■■■■■■■■■■■■■ 打包完成 ■■■■■■■■■■■■■■■■■■■■■■"
for /r . %%a in (unpkgs\*.nupkg) do (
    if "%1"=="" (
        md "d:\\nuget"
		echo "■■■■■■■■■■■■■■■■■■■■■■ 安装到本地: d:\nuget ■■■■■■■■■■■■■■■■■■■■■■"
        dotnet nuget push "%%a" -s "d:\\nuget"
    ) else (
        set Key=%1
        if %Key:~1,1%==: (
            md "%1%"
			echo "■■■■■■■■■■■■■■■■■■■■■■ 安装到本地: %1% ■■■■■■■■■■■■■■■■■■■■■■"
            dotnet nuget push "%%a" -s "%1%"
        ) else (
            dotnet nuget push "%%a" -s https://api.nuget.org/v3/index.json -k %1%
        )
    )
)
echo "■■■■■■■■■■■■■■■■■■■■■■ 安装完成 ■■■■■■■■■■■■■■■■■■■■■■"
del %CD%\unpkgs\*.nupkg