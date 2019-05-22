if (-Not [System.Environment]::Is64BitOperatingSystem) {
    Read-Host -Prompt "This software only works on a 64-bit version of Windows. Press Enter to exit"
    Exit
}

Set-ExecutionPolicy -ExecutionPolicy Unrestricted -Scope Process

# Self-elevate the script if required
if (-Not ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] 'Administrator')) {
    if ([int](Get-CimInstance -Class Win32_OperatingSystem | Select-Object -ExpandProperty BuildNumber) -ge 6000) {
        Read-Host -Prompt "The installer will now ask you for Administrator rights. Press Enter to continue"
        $CommandLine = "-File `"" + $MyInvocation.MyCommand.Path + "`" " + $MyInvocation.UnboundArguments
        Start-Process -FilePath PowerShell.exe -Verb Runas -ArgumentList $CommandLine
        Exit
    }
}

# Check if WSL is installed
$wslStatus = Get-WindowsOptionalFeature -Online -FeatureName Microsoft-Windows-Subsystem-Linux
if ($wslStatus.State -ne "Enabled") {
    Write-Output "The Windows Subsystem for Linux doesn't seem to be installed on this machine."
    $confirmation = Read-Host "Do you want me to try installing it right now? (y/n)"
    if ($confirmation -eq 'y') {
        # Install it if confirmed by the user
        Enable-WindowsOptionalFeature -Online -FeatureName Microsoft-Windows-Subsystem-Linux
    }
    Write-Output "Once WSL is installed, restart your computer and run this script again."
    Read-Host -Prompt "Press Enter to exit"
    Exit
}

# Sideload LRR distro package
Write-Output "Starting LANraragi for Windows Install/Update."
$appDataFolder = $env:APPDATA + "\LANraragi"
$lxRunLocation = $PSScriptRoot + "\LxRunOffline\LxRunOffline.exe"

Write-Output "Terminating potential existing instances"
wslconfig.exe /terminate lanraragi
Write-Output "Uninstalling previous WSL distro..."
Start-Process -NoNewWindow -Wait $lxRunLocation "ui -n lanraragi"
Write-Output "Installing new WSL distro from package.tar..."
$installArgs = "i -n lanraragi -d " + $appDataFolder + "\Distro -f " + $PSScriptRoot + "\package.tar" 
Start-Process -NoNewWindow -Wait $lxRunLocation $installArgs

# Copy Karen Bootloader to AppData
Write-Output "Installing Bootloader..."
$karenLocation = $PSScriptRoot + "\Bootloader"
Copy-Item $karenLocation $appDataFolder -Force -Recurse

# Add Shortcut
$objShell = New-Object -ComObject ("WScript.Shell")
$objShortCut = $objShell.CreateShortcut($env:APPDATA + "\Microsoft\Windows\Start Menu\Programs\LANraragi for Windows.lnk")
$objShortCut.TargetPath = $appDataFolder + "\Bootloader\Karen.exe" 
$objShortCut.Save()

# Good to go
Write-Output "All done! You can launch the program from your Start Menu."
Read-Host -Prompt "Press Enter to exit"
