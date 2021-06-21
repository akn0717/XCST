﻿param(
   $Configuration = "Debug"
)

$ErrorActionPreference = "Stop"
Push-Location (Split-Path $script:MyInvocation.MyCommand.Path)

$singleIndent = "   "
$indent = ""

function PushIndent {
   $script:indent = $indent + $singleIndent
}

function PopIndent {
   $script:indent = $indent.Substring($singleIndent.Length)
}

function WriteLine($line = "") {
   $indent + $line
}

function TestConfig($file) {

   $readerSettings = New-Object Xml.XmlReaderSettings
   $readerSettings.IgnoreComments = $true
   $readerSettings.IgnoreWhitespace = $true

   $reader = [Xml.XmlReader]::Create($file.FullName, $readerSettings)

   try {

      while ($reader.Read() `
         -and $reader.NodeType -ne [Xml.XmlNodeType]::Element) {
         
         if ($reader.NodeType -eq [Xml.XmlNodeType]::ProcessingInstruction `
            -and $reader.LocalName -eq "xcst-test") {

            $piValue = $reader.Value

            if (![string]::IsNullOrEmpty($piValue)) {
               return ([xml]"<xcst-test $piValue />").DocumentElement
            }

            break
         }
      }

   } finally {
      $reader.Close()
   }

   return ([xml]"<xcst-test/>").DocumentElement
}

function GenerateTests {

   Add-Type -Path ..\..\src\Xcst.Compiler\bin\$Configuration\Xcst.Compiler.dll

   $compilerFactory = New-Object Xcst.Compiler.XcstCompilerFactory

@"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
using System;
using System.Linq;
using TestFx = NUnit.Framework;
using static Xcst.Tests.TestsHelper;

#nullable enable
"@

   foreach ($subDirectory in ls -Directory) {
      GenerateTestsForDirectory $subDirectory $subDirectory.Name
   }
}

function GenerateTestsForDirectory([IO.DirectoryInfo]$directory, [string]$relativeNs) {

   $ns = "Xcst.Tests.$relativeNs"

   foreach ($file in ls "$($directory.FullName)\*" -Include *.xcst, *.pxcst -Exclude *.?.xcst, _*.xcst) {

      $compiler = $compilerFactory.CreateCompiler()
      $compiler.TargetClass = [IO.Path]::GetFileNameWithoutExtension($file.Name)
      $compiler.TargetNamespace = $ns
      $compiler.TargetVisibility = 'Public'
      $compiler.IndentChars = $singleIndent
      $compiler.NullableAnnotate = $true

      #Write-Host $file.FullName
      $xcstResult = $compiler.Compile((New-Object Uri $file.FullName))

      foreach ($src in $xcstResult.CompilationUnits) {
         $src
      }
   }

   $tests = ls $directory.FullName *.?.xcst

   if ($tests.Length -gt 0) {

      WriteLine
      WriteLine "namespace $ns {"
      PushIndent
   
      WriteLine
      WriteLine "[TestFx.TestFixture]"
      WriteLine "public partial class $($directory.Name)Tests {"
      PushIndent

      foreach ($file in $tests) { 

         $fileName = [IO.Path]::GetFileNameWithoutExtension($file.Name)
         $fail = $fileName -like '*.f'
         $correct = $fail -or $fileName -like '*.c'
         $testName = ($fileName -replace '[.-]', '_') -creplace '([a-z])([A-Z])', '$1_$2'
         $assertThrows = !$correct -or $fail

         $config = TestConfig($file)

         WriteLine
         WriteLine "#line 1 ""$($file.FullName)"""
         WriteLine "[TestFx.Test, TestFx.Category(""$relativeNs"")]"

         if ($config.ignore -eq "true") {
            WriteLine "[TestFx.Ignore("""")]"
         }

         WriteLine "public void $testName() {"
         PushIndent

         $disableWarning = if ($config.HasAttribute("disable-warning")) {
            """$($config.GetAttribute("disable-warning"))"""
            } else { "null" }

         $languageVersion = if ($config.HasAttribute("language-version")) {
            "$($config.GetAttribute("language-version"))m"
            } else { "-1m" }

         $testCall = "RunXcstTest(@""$($file.FullName)"", ""$testName"", ""$ns"", correct: $($correct.ToString().ToLower()), fail: $($fail.ToString().ToLower()), languageVersion: $languageVersion, disableWarning: $disableWarning)"

         if ($assertThrows) {

            $testException = if ($config.exception -ne $null) {
               $config.exception
            } elseif (!$correct) {
               "Xcst.Compiler.CompileException"
            } else {
               "Xcst.RuntimeException"
            }

            WriteLine "TestFx.Assert.Throws<$testException>(() => $testCall);"
         } else {
            WriteLine ($testCall + ";")
         }

         PopIndent
         WriteLine "}"
         WriteLine "#line default"
      }

      PopIndent
      WriteLine "}"

      PopIndent
      WriteLine "}"
   }

   foreach ($subDirectory in ls $directory.FullName -Directory) {
      GenerateTestsForDirectory $subDirectory ($relativeNs + "." + $subDirectory.Name)
   }
}

try {

   GenerateTests | Out-File Tests.generated.cs -Encoding utf8

} finally {
   Pop-Location
}
