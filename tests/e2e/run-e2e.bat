@echo off
powershell -ExecutionPolicy Bypass -Command "Set-Location 'd:\lib4_project\libr4\tests\e2e'; .\Run-FullFlowE2E.ps1 | Out-File 'C:\Temp\e2e-result.log' -Encoding UTF8"
