param (
   [string]$filePath = "myfile2.txt",
   [string]$adminPass = "degisti"
)
Write-Host "denemeler devam ediyor"
$scriptPath = split-path -parent $MyInvocation.MyCommand.Definition
Write-Host "$scriptPath"
New-Item "$scriptPath\$filePath" -type file

$appConfigFile = "$scriptPath\mydesktopproj.exe.config"
# initialize the xml object
$appConfig = New-Object XML
# load the config file as an xml object
$appConfig.Load($appConfigFile)
# iterate over the settings
foreach($app in $appConfig.configuration.appSettings.add)
{
    # write the name to the console
    'value: ' + $app.value
    # write the connection string to the console
    'key: ' + $app.key
    # change the connection string
    $app.value = $adminPass
}
# save the updated config file
$appConfig.Save($appConfigFile)