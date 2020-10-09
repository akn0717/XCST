﻿$ErrorActionPreference = "Stop"
Push-Location (Split-Path $script:MyInvocation.MyCommand.Path)

$solutionPath = Resolve-Path ..

try {

   $nuget = .\ensure-nuget.ps1
   &$nuget restore $solutionPath\XCST.sln

} finally {
   Pop-Location
}
