$sdkUrl = "https://builds.dotnet.microsoft.com/dotnet/Sdk/8.0.420/dotnet-sdk-8.0.420-win-x64.exe"
$target = Join-Path $env:TEMP "dotnet-sdk-8.0.420-win-x64.exe"
Invoke-WebRequest -Uri $sdkUrl -OutFile $target
Start-Process -FilePath $target -ArgumentList "/install /quiet /norestart" -Wait
