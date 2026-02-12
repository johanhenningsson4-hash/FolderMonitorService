# MailHog Integration Guide for .NET Framework 4.7.2

MailHog is an email testing tool for developers that runs a fake SMTP server to catch and display all outgoing emails in a web interface. This guide shows how to integrate your FolderMonitorService with MailHog for email testing and development.

## What is MailHog?

MailHog is a lightweight email testing tool that:
- ✅ **Catches all emails** sent by your application
- ✅ **Displays emails** in a user-friendly web interface
- ✅ **No external dependencies** - runs locally
- ✅ **Cross-platform** - Windows, macOS, Linux
- ✅ **API support** for automated testing
- ✅ **Zero configuration** for basic usage

## Installation Methods

### Method 1: Download Binary (Recommended)

1. **Download MailHog** from [GitHub Releases](https://github.com/mailhog/MailHog/releases)
   ```
   https://github.com/mailhog/MailHog/releases/latest
   ```

2. **For Windows**: Download `MailHog_windows_amd64.exe`

3. **Rename** the file to `mailhog.exe` (optional)

4. **Place in PATH** or create a dedicated folder like `C:\Tools\MailHog\`

### Method 2: Using Go (if you have Go installed)

```bash
go install github.com/mailhog/MailHog@latest
```

### Method 3: Using Docker

```bash
docker run -d -p 1025:1025 -p 8025:8025 mailhog/mailhog
```

## Running MailHog

### Windows Command Line

```cmd
# Navigate to MailHog directory
cd C:\Tools\MailHog

# Run MailHog
mailhog.exe
```

### PowerShell

```powershell
# Run MailHog
.\mailhog.exe

# Or if in PATH
mailhog
```

### Default Configuration

- **SMTP Server**: `localhost:1025`
- **Web Interface**: `http://localhost:8025`
- **API Endpoint**: `http://localhost:8025/api`

## App.config Configuration

Update your `App.config` to use MailHog SMTP server for development:

### Development Configuration

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <appSettings>
    <!-- MailHog SMTP Configuration for Development -->
    <add key="SmtpServer" value="localhost" />
    <add key="SmtpPort" value="1025" />
    <add key="SmtpUsername" value="" />
    <add key="SmtpPassword" value="" />
    <add key="SmtpEnableSsl" value="false" />
    <add key="SmtpUseDefaultCredentials" value="false" />
    
    <!-- Email Settings -->
    <add key="FromEmail" value="noreply@foldermonitorservice.local" />
    <add key="FromName" value="Folder Monitor Service (Dev)" />
    <add key="ToEmail" value="test@example.com" />
    
    <!-- Environment Flag -->
    <add key="Environment" value="Development" />
  </appSettings>
  
  <system.net>
    <mailSettings>
      <smtp from="noreply@foldermonitorservice.local">
        <network 
          host="localhost" 
          port="1025" 
          userName="" 
          password="" 
          enableSsl="false" 
          defaultCredentials="false" />
      </smtp>
    </mailSettings>
  </system.net>
</configuration>
```

### Production vs Development Configuration

Create separate config files or use app settings transformation:

```xml
<!-- Use conditional configuration -->
<appSettings>
  <!-- Production Gmail Settings -->
  <add key="SmtpServer" value="smtp.gmail.com" />
  <add key="SmtpPort" value="587" />
  <add key="SmtpEnableSsl" value="true" />
  
  <!-- MailHog Development Settings -->
  <!-- 
  <add key="SmtpServer" value="localhost" />
  <add key="SmtpPort" value="1025" />
  <add key="SmtpEnableSsl" value="false" />
  -->
</appSettings>
```

## C# Email Service Implementation

Here's an enhanced email service that works with both Gmail and MailHog:

```csharp
using System;
using System.Configuration;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace FolderMonitorService
{
    public class EmailService
    {
        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly string _username;
        private readonly string _password;
        private readonly bool _enableSsl;
        private readonly string _fromEmail;
        private readonly string _fromName;
        private readonly bool _isDevelopment;

        public EmailService()
        {
            _smtpServer = ConfigurationManager.AppSettings["SmtpServer"];
            _smtpPort = int.Parse(ConfigurationManager.AppSettings["SmtpPort"]);
            _username = ConfigurationManager.AppSettings["SmtpUsername"] ?? "";
            _password = DecodeBase64(ConfigurationManager.AppSettings["SmtpPassword"]) ?? "";
            _enableSsl = bool.Parse(ConfigurationManager.AppSettings["SmtpEnableSsl"]);
            _fromEmail = ConfigurationManager.AppSettings["FromEmail"];
            _fromName = ConfigurationManager.AppSettings["FromName"];
            _isDevelopment = ConfigurationManager.AppSettings["Environment"] == "Development";
        }

        public void SendFileChangeNotification(string filePath, string changeType)
        {
            var subject = $"[{(_isDevelopment ? "DEV" : "PROD")}] File Change Detected: {changeType}";
            var body = CreateFileChangeEmailBody(filePath, changeType);
            var toEmail = ConfigurationManager.AppSettings["ToEmail"];

            SendEmail(toEmail, subject, body);
        }

        public void SendEmail(string to, string subject, string body, bool isHtml = false)
        {
            try
            {
                using (var client = new SmtpClient(_smtpServer, _smtpPort))
                {
                    // Configure SMTP client
                    if (!string.IsNullOrEmpty(_username))
                    {
                        client.Credentials = new NetworkCredential(_username, _password);
                    }
                    client.EnableSsl = _enableSsl;
                    client.DeliveryMethod = SmtpDeliveryMethod.Network;

                    using (var message = new MailMessage())
                    {
                        message.From = new MailAddress(_fromEmail, _fromName);
                        message.To.Add(to);
                        message.Subject = subject;
                        message.Body = body;
                        message.IsBodyHtml = isHtml;

                        // Add development headers for MailHog
                        if (_isDevelopment)
                        {
                            message.Headers.Add("X-Environment", "Development");
                            message.Headers.Add("X-MailHog-Test", "true");
                        }

                        client.Send(message);
                        
                        Console.WriteLine($"Email sent successfully to {to}");
                        if (_isDevelopment)
                        {
                            Console.WriteLine("Check MailHog web interface: http://localhost:8025");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send email: {ex.Message}");
                throw;
            }
        }

        private string CreateFileChangeEmailBody(string filePath, string changeType)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var environment = _isDevelopment ? "DEVELOPMENT" : "PRODUCTION";
            
            return $@"
File Monitor Alert - {environment}

Timestamp: {timestamp}
Change Type: {changeType}
File Path: {filePath}
Server: {Environment.MachineName}

This is an automated message from Folder Monitor Service.

{(_isDevelopment ? "This email was sent to MailHog for testing." : "")}
";
        }

        private string DecodeBase64(string encodedValue)
        {
            if (string.IsNullOrEmpty(encodedValue))
                return encodedValue;

            try
            {
                var data = Convert.FromBase64String(encodedValue);
                return Encoding.UTF8.GetString(data);
            }
            catch
            {
                // Return as-is if not Base64 encoded
                return encodedValue;
            }
        }
    }
}
```

## Testing with MailHog

### 1. Start MailHog

```cmd
# Start MailHog
mailhog.exe
```

You should see output like:
```
2024/01/15 10:30:00 Using in-memory storage
2024/01/15 10:30:00 [SMTP] Binding to address: 0.0.0.0:1025
2024/01/15 10:30:00 [HTTP] Binding to address: 0.0.0.0:8025
```

### 2. Test Email Sending

```csharp
// Example usage in your service
class Program
{
    static void Main(string[] args)
    {
        try
        {
            var emailService = new EmailService();
            
            // Test email
            emailService.SendEmail(
                "test@example.com", 
                "MailHog Test Email", 
                "This is a test email sent to MailHog!"
            );

            // Test file change notification
            emailService.SendFileChangeNotification(
                @"C:\TestFolder\document.txt", 
                "Created"
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}
```

### 3. View Emails in MailHog

1. **Open web browser** and navigate to: `http://localhost:8025`
2. **View captured emails** in the MailHog interface
3. **Inspect email content**, headers, and attachments
4. **Delete emails** or clear all messages

## Advanced MailHog Features

### Environment Variables Configuration

Set environment variables to configure MailHog:

```cmd
# Set custom ports
set MH_SMTP_BIND_ADDR=0.0.0.0:2025
set MH_UI_BIND_ADDR=0.0.0.0:9025

# Run MailHog with custom config
mailhog.exe
```

### API Integration for Testing

```csharp
public class MailHogApiClient
{
    private readonly string _baseUrl = "http://localhost:8025/api";

    public async Task<int> GetMessageCountAsync()
    {
        using (var client = new HttpClient())
        {
            var response = await client.GetStringAsync($"{_baseUrl}/v2/messages");
            // Parse JSON response to count messages
            // Implementation depends on JSON parsing library
            return 0; // placeholder
        }
    }

    public async Task ClearAllMessagesAsync()
    {
        using (var client = new HttpClient())
        {
            await client.DeleteAsync($"{_baseUrl}/v1/messages");
        }
    }
}
```

## Integration with Windows Service

Add email notifications to your FolderMonitorService:

```csharp
public partial class FolderMonitorService : ServiceBase
{
    private FileSystemWatcher _watcher;
    private EmailService _emailService;

    protected override void OnStart(string[] args)
    {
        _emailService = new EmailService();
        _watcher = new FileSystemWatcher(@"C:\MonitorFolder");
        
        _watcher.Created += OnFileCreated;
        _watcher.Changed += OnFileChanged;
        _watcher.Deleted += OnFileDeleted;
        
        _watcher.EnableRaisingEvents = true;
        
        // Send startup notification
        _emailService.SendEmail(
            ConfigurationManager.AppSettings["ToEmail"],
            "Folder Monitor Service Started",
            "The folder monitoring service has started successfully."
        );
    }

    private void OnFileCreated(object sender, FileSystemEventArgs e)
    {
        _emailService.SendFileChangeNotification(e.FullPath, "Created");
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        _emailService.SendFileChangeNotification(e.FullPath, "Modified");
    }

    private void OnFileDeleted(object sender, FileSystemEventArgs e)
    {
        _emailService.SendFileChangeNotification(e.FullPath, "Deleted");
    }
}
```

## Development vs Production Setup

### 1. Use Configuration Transformation

Create `App.Debug.config`:

```xml
<?xml version="1.0"?>
<configuration xmlns:xdt="http://schemas.microsoft.com/XML-Document-Transform">
  <appSettings>
    <add key="SmtpServer" value="localhost" xdt:Transform="SetAttributes" xdt:Locator="Match(key)" />
    <add key="SmtpPort" value="1025" xdt:Transform="SetAttributes" xdt:Locator="Match(key)" />
    <add key="SmtpEnableSsl" value="false" xdt:Transform="SetAttributes" xdt:Locator="Match(key)" />
    <add key="Environment" value="Development" xdt:Transform="SetAttributes" xdt:Locator="Match(key)" />
  </appSettings>
</configuration>
```

### 2. Environment-Based Configuration

```csharp
public class EmailConfiguration
{
    public static EmailConfig GetConfig()
    {
        var isDevelopment = ConfigurationManager.AppSettings["Environment"] == "Development";
        
        if (isDevelopment)
        {
            return new EmailConfig
            {
                SmtpServer = "localhost",
                Port = 1025,
                EnableSsl = false,
                Username = "",
                Password = ""
            };
        }
        else
        {
            return new EmailConfig
            {
                SmtpServer = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                Username = ConfigurationManager.AppSettings["SmtpUsername"],
                Password = DecodeBase64(ConfigurationManager.AppSettings["SmtpPassword"])
            };
        }
    }
}
```

## Troubleshooting

### Common Issues

1. **"Connection refused" error**
   - Ensure MailHog is running
   - Check if port 1025 is available
   - Verify firewall settings

2. **Emails not appearing in MailHog**
   - Check MailHog console for error messages
   - Verify SMTP configuration (host: localhost, port: 1025)
   - Ensure SSL is disabled for MailHog

3. **MailHog web interface not accessible**
   - Check if port 8025 is available
   - Try `http://127.0.0.1:8025` instead of `localhost`
   - Verify no proxy settings interfere

### Debugging Tips

```csharp
public void TestEmailConfiguration()
{
    try
    {
        using (var client = new SmtpClient("localhost", 1025))
        {
            client.EnableSsl = false;
            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            
            using (var message = new MailMessage(
                "test@example.com", 
                "recipient@example.com", 
                "Test Subject", 
                "Test Body"))
            {
                client.Send(message);
                Console.WriteLine("Test email sent successfully!");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Email test failed: {ex.Message}");
    }
}
```

## Best Practices

1. **Use MailHog for Development Only**
   - Never use MailHog in production
   - Switch to real SMTP servers for production

2. **Environment Detection**
   - Implement proper environment detection
   - Use configuration transformations

3. **Email Templates**
   - Create reusable email templates
   - Include environment indicators in development emails

4. **Testing Automation**
   - Use MailHog API for automated testing
   - Clear messages between tests

5. **Security**
   - MailHog has no authentication - use only in secure development environments
   - Don't expose MailHog ports to external networks

## Conclusion

MailHog provides an excellent solution for testing email functionality in your .NET Framework 4.7.2 FolderMonitorService project. It allows you to:

- Test email sending without affecting real recipients
- Debug email formatting and content
- Verify SMTP configuration
- Develop email features safely

This setup ensures your email notifications work correctly before deploying to production with real SMTP providers like Gmail.