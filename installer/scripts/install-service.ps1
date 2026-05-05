param(
  [Parameter(Mandatory = $true)]
  [string]$InstallRoot
)

$serviceName = "SamerHubService"
$serviceExe = Join-Path $InstallRoot "service\SamerHub.Service.exe"

if (-not (Test-Path $serviceExe)) {
  throw "Service executable bulunamadi: $serviceExe"
}

if (Get-Service -Name $serviceName -ErrorAction SilentlyContinue) {
  Stop-Service -Name $serviceName -ErrorAction SilentlyContinue
  sc.exe delete $serviceName | Out-Null
  Start-Sleep -Seconds 1
}

sc.exe create $serviceName binPath= "`"$serviceExe`"" start= auto DisplayName= "SAMER Hub Service" | Out-Null
sc.exe description $serviceName "Local backend for SAMER Hub desktop" | Out-Null
sc.exe start $serviceName | Out-Null
