param([string]$OutputFile = '../FocusClock.exe')
$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$framework = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319'
$destination = Join-Path $PSScriptRoot $OutputFile
Push-Location -LiteralPath $PSScriptRoot
try {
& $compiler /nologo /target:winexe /platform:anycpu /optimize+ /utf8output "/out:$OutputFile" /win32manifest:app.manifest /win32icon:app.ico "/reference:$framework/WPF/PresentationFramework.dll" "/reference:$framework/WPF/PresentationCore.dll" "/reference:$framework/WPF/WindowsBase.dll" /reference:System.Xaml.dll /reference:System.Windows.Forms.dll /reference:System.Drawing.dll FocusClock.cs Localization.cs
} finally { Pop-Location }
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
Write-Output $destination
