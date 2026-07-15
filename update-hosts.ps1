# Update hosts file to point invoice to new VM IP
$hostsPath = "C:\Windows\System32\drivers\etc\hosts"
$content = Get-Content $hostsPath
$updated = $content -replace '192\.168\.1\.128 invoice', '192.168.1.124 invoice'
$updated | Set-Content $hostsPath -Force
Write-Host "Updated hosts file: 192.168.1.128 -> 192.168.1.124" -ForegroundColor Green
