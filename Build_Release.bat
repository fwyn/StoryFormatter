@Echo Off
PushD %~dp0

Rem Look for Microsoft Build Tools 2015.
Set BuildPath=NotFound
If Exist "%ProgramFiles(x86)%\MSBuild\14.0\Bin\MSBuild.exe" Set BuildPath=%ProgramFiles(x86)%\MSBuild\14.0\Bin
If Exist      "%ProgramFiles%\MSBuild\14.0\Bin\MSBuild.exe" Set      BuildPath=%ProgramFiles%\MSBuild\14.0\Bin
If Exist "%ProgramFiles(x86)%\Microsoft Visual Studio\2017\BuildTools\MSBuild\15.0\Bin\MSBuild.exe" Set BuildPath=%ProgramFiles(x86)%\Microsoft Visual Studio\2017\BuildTools\MSBuild\15.0\Bin
If Exist  "%ProgramFiles(x86)%\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\MSBuild.exe" Set  BuildPath=%ProgramFiles(x86)%\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin
If Exist "%ProgramFiles(x86)%\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin\MSBuild.exe" Set BuildPath=%ProgramFiles(x86)%\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin
If "%BuildPath%" == "NotFound" (
	Echo Microsoft Build Tools 2015 not found.
	Echo Download it here:
	Echo https://www.microsoft.com/en-us/download/details.aspx?id=48159
	If Defined ErrorNoPause Exit 187
	Pause
	Exit 187
)
Echo Using Build Tools Path: %BuildPath%
Set Path=%BuildPath%;%Path%


msbuild src\StoryFormatter\StoryFormatter.csproj /t:Clean;Build /p:Configuration=Release


If Not Exist Release MkDir Release
Copy /D src\StoryFormatter\bin\Release\StoryFormatter.exe Release\

Pause
PopD
Goto :EOF
