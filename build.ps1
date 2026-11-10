# Build only the offline desktop application; no downloads or firmware actions.
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$exitCode = 1
$candidateDirectory = $null

try {
    $root = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
    $sourceDirectory = Join-Path $root 'builder_app'
    $outputDirectory = Join-Path $root 'bin'
    $outputFile = Join-Path $outputDirectory 'XIO_Puzzle_Builder.exe'
    $windowsDirectory = [Environment]::GetFolderPath([Environment+SpecialFolder]::Windows)
    if ([string]::IsNullOrWhiteSpace($windowsDirectory)) {
        throw 'Windows and .NET Framework 4.8 are required.'
    }

    $frameworkDirectory = Join-Path $windowsDirectory 'Microsoft.NET\Framework64\v4.0.30319'
    $compiler = Join-Path $frameworkDirectory 'csc.exe'
    if (-not (Test-Path -LiteralPath $compiler -PathType Leaf)) {
        # Framework is the 32-bit directory; do not fall back after compiler errors.
        $frameworkDirectory = Join-Path $windowsDirectory 'Microsoft.NET\Framework\v4.0.30319'
        $compiler = Join-Path $frameworkDirectory 'csc.exe'
    }
    if (-not (Test-Path -LiteralPath $compiler -PathType Leaf)) {
        throw 'The local .NET Framework compiler was not found. Install .NET Framework 4.8 before building.'
    }

    $referenceNames = @(
        'System.dll',
        'System.Core.dll',
        'System.Web.Extensions.dll',
        'System.Security.dll',
        'System.Xaml.dll',
        'WPF\WindowsBase.dll',
        'WPF\PresentationCore.dll',
        'WPF\PresentationFramework.dll',
        'WPF\UIAutomationTypes.dll'
    )
    $referenceArguments = @()
    foreach ($name in $referenceNames) {
        $reference = Join-Path $frameworkDirectory $name
        if (-not (Test-Path -LiteralPath $reference -PathType Leaf)) {
            throw ('Required local framework assembly is missing: ' + $reference)
        }
        $referenceArguments += ('/reference:' + $reference)
    }
    $sources = @(Get-ChildItem -LiteralPath $sourceDirectory -Filter '*.cs' -File | Sort-Object Name)
    if ($sources.Count -eq 0) {
        throw ('No C# sources found in ' + $sourceDirectory)
    }

    if (-not (Test-Path -LiteralPath $outputDirectory -PathType Container)) {
        New-Item -ItemType Directory -Path $outputDirectory | Out-Null
    }
    # Keep the real assembly name inside a unique staging directory.
    $candidateDirectory = Join-Path $outputDirectory ('.build-' + [Guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $candidateDirectory | Out-Null
    $candidateFile = Join-Path $candidateDirectory 'XIO_Puzzle_Builder.exe'
    $compilerArguments = @(
        '/nologo',
        '/target:winexe',
        '/optimize+',
        '/debug-',
        '/langversion:5',
        '/platform:anycpu',
        '/main:XioPuzzleStudio.Program',
        ('/out:' + $candidateFile)
    ) + $referenceArguments + @($sources | ForEach-Object { $_.FullName })

    & $compiler @compilerArguments
    $compilerExitCode = $LASTEXITCODE
    if ($compilerExitCode -ne 0) {
        $exitCode = $compilerExitCode
        [Console]::Error.WriteLine('Compilation failed; the existing executable was not replaced.')
    }
    else {
        if (-not (Test-Path -LiteralPath $candidateFile -PathType Leaf)) {
            throw 'The compiler reported success but did not create the candidate executable.'
        }
        if (Test-Path -LiteralPath $outputFile -PathType Leaf) {
            [System.IO.File]::Replace($candidateFile, $outputFile, [System.Management.Automation.Language.NullString]::Value)
        }
        else {
            [System.IO.File]::Move($candidateFile, $outputFile)
        }
        $exitCode = 0
        Write-Host ('Built: ' + $outputFile)
    }
}
catch {
    [Console]::Error.WriteLine($_.Exception.Message)
}
finally {
    if ($null -ne $candidateDirectory -and (Test-Path -LiteralPath $candidateDirectory)) {
        try {
            Remove-Item -LiteralPath $candidateDirectory -Recurse -Force -ErrorAction Stop
        }
        catch {
            [Console]::Error.WriteLine('Could not remove temporary build directory: ' + $candidateDirectory)
            if ($exitCode -eq 0) { $exitCode = 1 }
        }
    }
}

exit $exitCode
