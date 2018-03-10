# First-time setup for building Firestone
# Written by Katy Coe 2018
# https://github.com/HearthCode/Firestone
# http://www.hearthcode.org

param(
	[string]$protoPath = "",
	[string]$domain = "localhost"
)

Write-Host "Firestone first-time build setup"

# Make directories for proto files and compiled C# protos
Write-Host "Creating protobuf directories if needed..."
New-Item protos -ItemType Directory -Force | Out-Null
New-Item Firestone\protobuf -ItemType Directory -Force | Out-Null

# Download Google Protobuf if we don't already have it
if (-not (Test-Path 'protoc.exe')) {
	Write-Host "Downloading Google Protobuf..."
	[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
	$ProgressPreference = 'silentlyContinue'
	Invoke-WebRequest https://github.com/google/protobuf/releases/download/v3.5.1/protoc-3.5.1-win32.zip -OutFile protoc.zip
	& { Add-Type -A 'System.IO.Compression.FileSystem'; [IO.Compression.ZipFile]::ExtractToDirectory('protoc.zip', 'protoc'); }
	copy protoc\bin\protoc.exe .
	Remove-Item -Recurse -Force protoc
	Remove-Item protoc.zip
} else {
	Write-Host "Found existing Google Protobuf binary"
}

# Copy proto files to the solution
# NOTE: This does not remove old proto files
if (-not ([string]::IsNullOrEmpty($protoPath))) {
	Write-Host "Copying proto files..."
	Copy-Item -Path $protoPath\* -Recurse -Destination protos -Force
} else {
	Write-Host "Using existing proto files"
}

# Compile proto files
# NOTE: The protos must be in proto3 syntax with option csharp_namespace or this will fail
Write-Host "Compiling proto files..."
Get-ChildItem protos -Filter *.proto | ForEach-Object {
	Write-Host -NoNewline "Compiling $_..."
	$result = ./protoc.exe -I=protos --csharp_out=Firestone\protobuf $_.Name 2>&1
	if ($?) {
		Write-Host " succeeded"
	} else {
		Write-Host " failed with error: $result"
		Exit
	}
}

# Generate SSL certificate
if (-not (Test-Path 'Firestone\Firestone.pfx')) {
	Write-Host "Generating SSL certificate..."
	$OurPath = Split-Path $MyInvocation.InvocationName
	Push-Location $OurPath\Firestone\utils
	./gen-certificate.ps1 -domain $domain | Out-Null
	Pop-Location
} else {
	Write-Host "Using existing SSL certificate"
}