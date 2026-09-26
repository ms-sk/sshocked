#Requires -RunAsAdministrator

$Repo = "ms-sk/sshocked"
$Binary = "ssk.exe"
$InstallDir = "$env:ProgramFiles\sshocked"

function Info  { Write-Host "==>" -ForegroundColor Blue -NoNewline; Write-Host " $args" }
function Ok    { Write-Host " ✓" -ForegroundColor Green -NoNewline; Write-Host " $args" }
function Die   { Write-Host "!! $args" -ForegroundColor Red; exit 1 }

$Arch = switch ([Environment]::GetEnvironmentVariable("PROCESSOR_ARCHITECTURE")) {
  "AMD64" { "x64" }
  "ARM64" { "arm64" }
  default { Die "Unsupported architecture: $_. Supported: AMD64, ARM64." }
}

$Rid = "win-$Arch"
Info "Detected: $Rid"

Info "Fetching latest release..."
$Release = Invoke-RestMethod "https://api.github.com/repos/$Repo/releases/latest" -Headers @{ Accept = "application/json" }
$Tag = $Release.tag_name
if (-not $Tag) { Die "Could not determine the latest release tag." }

$DownloadUrl = "https://github.com/$Repo/releases/download/$Tag/sshocked-$Rid.zip"
$ArchivePath = "$env:TEMP\sshocked-$Rid.zip"
$ExtractPath = "$env:TEMP\sshocked-install"

Info "Downloading sshocked $Tag..."
Invoke-WebRequest -Uri $DownloadUrl -OutFile $ArchivePath

Info "Extracting..."
Remove-Item -Recurse -Force $ExtractPath -ErrorAction SilentlyContinue
Expand-Archive -Path $ArchivePath -DestinationPath $ExtractPath

Info "Installing to $InstallDir..."
New-Item -ItemType Directory -Force -Path $InstallDir | Out-Null
Copy-Item -Path "$ExtractPath\$Binary" -Destination "$InstallDir\$Binary" -Force

$UserPath = [Environment]::GetEnvironmentVariable("PATH", "User")
if ($UserPath -notlike "*$InstallDir*") {
  [Environment]::SetEnvironmentVariable("PATH", "$UserPath;$InstallDir", "User")
  $env:PATH += ";$InstallDir"
}

Remove-Item -Recurse -Force $ExtractPath, $ArchivePath -ErrorAction SilentlyContinue

Ok "sshocked $Tag installed to $InstallDir\$Binary"
Info "Run 'ssk --help' to get started."
