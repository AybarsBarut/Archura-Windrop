#Requires -RunAsAdministrator
[CmdletBinding()]
param(
    [ValidateRange(1024, 65535)]
    [int]$TcpPort = 8631
)

$ErrorActionPreference = 'Stop'
$tcpName = 'Archura Windrop IPP'
$mdnsName = 'Archura Windrop mDNS'

$existingTcp = Get-NetFirewallRule -DisplayName $tcpName -ErrorAction SilentlyContinue
if ($null -ne $existingTcp) {
    Remove-NetFirewallRule -DisplayName $tcpName
}

$existingMdns = Get-NetFirewallRule -DisplayName $mdnsName -ErrorAction SilentlyContinue
if ($null -ne $existingMdns) {
    Remove-NetFirewallRule -DisplayName $mdnsName
}

New-NetFirewallRule -DisplayName $tcpName -Direction Inbound -Action Allow -Protocol TCP -LocalPort $TcpPort -Profile Private,Public -RemoteAddress LocalSubnet | Out-Null
New-NetFirewallRule -DisplayName $mdnsName -Direction Inbound -Action Allow -Protocol UDP -LocalPort 5353 -Profile Private,Public -RemoteAddress LocalSubnet | Out-Null

Write-Host "Windrop firewall rules installed for TCP $TcpPort and UDP 5353, restricted to the local subnet."
