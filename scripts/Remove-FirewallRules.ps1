#Requires -RunAsAdministrator
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Remove-NetFirewallRule -DisplayName 'Archura Windrop IPP' -ErrorAction SilentlyContinue
Remove-NetFirewallRule -DisplayName 'Archura Windrop mDNS' -ErrorAction SilentlyContinue
Write-Host 'Windrop firewall rules removed.'
