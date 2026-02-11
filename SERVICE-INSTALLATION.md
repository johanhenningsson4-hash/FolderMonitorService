# Windows Service Installation Guide

## Automated Installation (Recommended)

### Using the MSI Installer

1. **Download** the latest `FolderMonitorServiceInstaller.msi` from the [Releases page](https://github.com/johanhenningsson4-hash/FolderMonitorService/releases)

2. **Run as Administrator** (Right-click ? "Run as administrator")

3. **Follow the Installation Wizard**:
   - Accept the license agreement
   - Choose installation directory (default: `C:\Program Files\FolderMonitorService\`)
   - Complete the installation

4. **Service Automatically Starts** after installation

### What the Installer Does

- ? Copies service files to Program Files
- ? Installs Windows Service (`FolderMonitorService`)
- ? Configures service to start automatically
- ? Sets up proper permissions
- ? Creates uninstaller entry in Programs and Features

## Manual Installation (Advanced Users)

### Prerequisites

- Windows 10/11 or Windows Server 2016+
- .NET Framework 4.7.2 or higher
- Administrator privileges

### Manual Service Registration

```cmd
# Register the service
sc create FolderMonitorService binpath= "C:\Path\To\FolderMonitorService.exe" start= auto

# Set service description
sc description FolderMonitorService "Monitors specified folders for file system changes"

# Start the service
sc start FolderMonitorService
```

### Manual Service Removal

```cmd
# Stop the service
sc stop FolderMonitorService

# Delete the service
sc delete FolderMonitorService
```

## Configuration

### Default Configuration

The service installs with default settings in `FolderMonitorService.exe.config`:

- **Monitor Path**: `C:\MonitorFolder` (create this folder or update config)
- **Log Level**: Information
- **SMTP**: Disabled (configure as needed)

### Post-Installation Configuration

1. **Stop the service**:
   ```cmd
   sc stop FolderMonitorService
   ```

2. **Edit configuration** at:
   ```
   C:\Program Files\FolderMonitorService\FolderMonitorService.exe.config
   ```

3. **Configure folder paths, SMTP settings, etc.**

4. **Start the service**:
   ```cmd
   sc start FolderMonitorService
   ```

## Service Management

### Using Windows Services Console

1. Press `Win + R`, type `services.msc`
2. Find "Folder Monitor Service"
3. Right-click for Start/Stop/Restart options

### Using Command Line

```cmd
# Check service status
sc query FolderMonitorService

# Start service
net start FolderMonitorService

# Stop service
net stop FolderMonitorService

# Restart service
net stop FolderMonitorService && net start FolderMonitorService
```

### Using PowerShell

```powershell
# Get service status
Get-Service FolderMonitorService

# Start service
Start-Service FolderMonitorService

# Stop service
Stop-Service FolderMonitorService

# Restart service
Restart-Service FolderMonitorService
```

## Troubleshooting

### Service Won't Start

1. **Check Event Logs**:
   - Windows Event Viewer ? Windows Logs ? Application
   - Look for "FolderMonitorService" entries

2. **Verify Permissions**:
   - Service runs as LocalSystem by default
   - Ensure monitored folders are accessible

3. **Check Configuration**:
   - Verify App.config syntax
   - Ensure monitored paths exist

### Configuration Issues

1. **Invalid Folder Paths**:
   - Create missing directories
   - Check path permissions
   - Use absolute paths

2. **SMTP Configuration**:
   - Verify Gmail app password
   - Test network connectivity
   - Check firewall settings

### Uninstallation Issues

1. **Use MSI Uninstaller**:
   - Programs and Features ? Folder Monitor Service ? Uninstall

2. **Manual Cleanup** (if needed):
   ```cmd
   sc stop FolderMonitorService
   sc delete FolderMonitorService
   # Remove installation directory manually
   ```

## Log Files

- **Service Logs**: Check Event Viewer (Application log)
- **Configuration**: `C:\Program Files\FolderMonitorService\`

## Support

For issues and questions:
1. Check the [GitHub Issues](https://github.com/johanhenningsson4-hash/FolderMonitorService/issues)
2. Review the Event Logs for error details
3. Verify system requirements are met

## Version History

- **v1.0.x**: Initial release with MSI installer
- Service auto-start configuration
- Gmail SMTP integration support
- Base64 password encoding