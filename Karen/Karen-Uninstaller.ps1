$appDataFolder = $env:APPDATA + "\LANraragi"

if (Read-Host "Do you want to uninstall LRR for Windows? (y/n)") {
    Write-Output "Terminating potential existing instances"
    wslconfig.exe /terminate lanraragi

    Write-Output "Uninstalling previous WSL distro..."
    wslconfig.exe /unregister lanraragi
    Remove-Item $appDataFolder -Force -Recurse

    Write-Output "Removing shortcuts and registry keys..."
    $shortcutLocation = $env:APPDATA + "\Microsoft\Windows\Start Menu\Programs\LANraragi for Windows.lnk"
    Remove-Item $shortcutLocation
    Remove-ItemProperty -Name 'Karen' -Path 'HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Run'

    # Good to go
    Write-Output "All done!"
    Read-Host -Prompt "Press Enter to exit"
}
