# PowerShell script to install FolderMonitorService as a Windows service

# Define service parameters
$serviceName = "FolderMonitorService"
$displayName = "Folder Monitor Service"
$description = "Monitors changes in a specified folder and logs them."
$exePath = (Get-Location).Path + "\bin\Release\FolderMonitorService.exe"

# Check if the service already exists
if (Get-Service -Name $serviceName -ErrorAction SilentlyContinue) {
    Write-Host "Service '$serviceName' already exists. Stopping and deleting it..."
    Stop-Service -Name $serviceName -Force -ErrorAction SilentlyContinue
    sc.exe delete $serviceName
}

# Create the service
Write-Host "Creating service '$serviceName'..."
sc.exe create $serviceName binPath= $exePath DisplayName= $displayName start= auto

# Set the service description
sc.exe description $serviceName "$description"

Write-Host "Service '$serviceName' installed successfully."