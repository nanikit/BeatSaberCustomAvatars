param(
    [Parameter()]
    [string] $WorkingDirectory,

    [Parameter(Mandatory)]
    [string[]] $Paths,

    [Parameter(Mandatory)]
    [string] $Output
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($WorkingDirectory)) {
    $WorkingDirectory = (Get-Location).Path
} elseif (![System.IO.Path]::IsPathRooted($WorkingDirectory)) {
    $WorkingDirectory = [System.IO.Path]::Join((Get-Location).Path, $WorkingDirectory)
}

$WorkingDirectory = [System.IO.Path]::GetFullPath($WorkingDirectory) + "\"

Write-Host $WorkingDirectory

$tempFolder = $env:TEMP + "\psunitypackage"

if (Test-Path $tempFolder) {
    Remove-Item $tempFolder -Recurse -Force
}

New-Item -Type directory -Path $tempFolder

foreach ($path in $Paths) {
    foreach ($metaFile in Get-ChildItem ($WorkingDirectory + $path) -Recurse) {
        if ($metaFile.Extension -ne ".meta") {
            continue
        }

        $yaml = Get-Content $metaFile.FullName | ConvertFrom-Yaml

        if ($yaml["folderAsset"] -eq 'yes') {
            continue
        }

        $assetFile = $metaFile.FullName.Substring(0, $metaFile.FullName.Length - 5)

        if (!(Test-Path $assetFile)) {
            continue
        }

        # create asset folder
        $assetFolderPath = [System.IO.Path]::Combine($tempFolder, $yaml["guid"])
        New-Item -Type directory -Path $assetFolderPath -Force

        # copy asset + meta to folder
        $assetFilePath = [System.IO.Path]::Join($assetFolderPath, "asset")
        $metaFilePath = [System.IO.Path]::Join($assetFolderPath, "asset.meta")
        $pathnameFilePath = [System.IO.Path]::Join($assetFolderPath, "pathname")
        Copy-Item $metaFile.FullName $metaFilePath
        Copy-Item $assetFile $assetFilePath
        Set-Content $pathnameFilePath $assetFile.Replace($WorkingDirectory, "").Replace("\", "/") -NoNewLine
    }
}

if (!$Output.EndsWith(".unitypackage")) {
    $Output += ".unitypackage"
}

if (Test-Path $Output) {
    Remove-Item $Output -Force
}

tar -czf $Output -C $tempFolder *

Remove-Item $tempFolder -Recurse -Force