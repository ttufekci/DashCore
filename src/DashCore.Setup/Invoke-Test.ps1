param (
   [string]$filePath = "myfile2.txt",
   [string]$adminPass = "degisti"
)

$scriptPath = split-path -parent $MyInvocation.MyCommand.Definition

$appsettingsjson = "$scriptPath\appsettings.json"

$a = Get-Content $appsettingsjson | ConvertFrom-Json

$a.AppSettings.AdminDefaultPass = "changed successfully"

$a | ConvertTo-Json | set-content $appsettingsjson
