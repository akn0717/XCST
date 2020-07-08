﻿
$ErrorActionPreference = "Stop"
Push-Location (Split-Path $script:MyInvocation.MyCommand.Path)

function CompressSource {

   # test compilation
   $saxonPath = Resolve-Path ..\..\..\packages\Saxon-HE.*
   $saxonCommand = "$saxonPath\tools\Transform.exe -nogo -jit:off -xsl:xcst-compile.xsl"
   $saxonOutput = cmd /c $saxonCommand '2>&1'

   if ($saxonOutput -ne $null) {
      $saxonOutput
      ""
      #"error: XSLT compilation failed."
      #return
   }

   Add-Type -AssemblyName System.IO.Compression

   try {

      $zipPath = Join-Path (Get-Item .) xcst-xsl.zip
      $zipFile = [IO.File]::Create($zipPath)

      try {

         $zipArchive = New-Object IO.Compression.ZipArchive($zipFile, [IO.Compression.ZipArchiveMode]::Create)

         foreach ($xsl in ls *.xsl -Exclude xcst-compile-cmdline.xsl) {

            $zipEntry = $zipArchive.CreateEntry($xsl.Name)
            $zipEntry.LastWriteTime = $xsl.LastWriteTime

            try {

               $source = [IO.File]::Open($xsl.FullName, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)

               try {

                  $entryOutput = $zipEntry.Open()
                  $source.CopyTo($entryOutput)

               } finally {
                  if ($entryOutput -ne $null) {
                     $entryOutput.Dispose()
                  }
               }

            } finally {
               if ($source -ne $null) {
                  $source.Dispose()
               }
            }
         }

      } finally {
         if ($zipArchive -ne $null) {
            $zipArchive.Dispose()
         }
      }

   } finally {
      if ($zipFile -ne $null) {
         $zipFile.Dispose()
      }
   }
}

try {
   CompressSource
} finally {
   Pop-Location
}
