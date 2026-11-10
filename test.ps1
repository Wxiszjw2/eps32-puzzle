Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

try {
    $root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
    $builder = Join-Path $root 'bin\XIO_Puzzle_Builder.exe'
    if (-not (Test-Path -LiteralPath $builder -PathType Leaf)) { throw 'Build the desktop application first.' }
    $windows = [Environment]::GetFolderPath([Environment+SpecialFolder]::Windows)
    $framework = Join-Path $windows 'Microsoft.NET\Framework64\v4.0.30319'
    if (-not (Test-Path -LiteralPath (Join-Path $framework 'csc.exe'))) {
        $framework = Join-Path $windows 'Microsoft.NET\Framework\v4.0.30319'
    }
    $output = Join-Path $root 'bin\Builder.Tests.exe'
    $arguments = @('/nologo', '/target:exe', '/langversion:5', '/debug-', ('/out:' + $output), ('/reference:' + $builder))
    foreach ($name in @('System.dll', 'System.Core.dll', 'System.Web.Extensions.dll', 'System.Security.dll', 'System.Xaml.dll', 'WPF\WindowsBase.dll', 'WPF\PresentationCore.dll', 'WPF\PresentationFramework.dll', 'WPF\UIAutomationTypes.dll')) {
        $arguments += '/reference:' + (Join-Path $framework $name)
    }
    $arguments += Join-Path $root 'tests\Builder.Tests.cs'
    & (Join-Path $framework 'csc.exe') @arguments
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    & $output
    exit $LASTEXITCODE
}
catch {
    [Console]::Error.WriteLine($_.Exception.Message)
    exit 1
}
