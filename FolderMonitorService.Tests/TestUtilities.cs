using System;
using System.IO;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FolderMonitorService.Tests.Utilities
{
    /// <summary>
    /// Utility class for common test operations and helpers
    /// </summary>
    public static class TestUtilities
    {
        /// <summary>
        /// Creates a temporary directory for testing
        /// </summary>
        /// <param name="prefix">Prefix for the directory name</param>
        /// <returns>Full path to the created directory</returns>
        public static string CreateTempDirectory(string prefix = "FolderMonitorTest")
        {
            var tempPath = Path.Combine(Path.GetTempPath(), $"{prefix}_{Guid.NewGuid()}");
            Directory.CreateDirectory(tempPath);
            return tempPath;
        }

        /// <summary>
        /// Safely deletes a directory and all its contents
        /// </summary>
        /// <param name="directoryPath">Path to the directory to delete</param>
        public static void SafeDeleteDirectory(string directoryPath)
        {
            if (Directory.Exists(directoryPath))
            {
                try
                {
                    Directory.Delete(directoryPath, true);
                }
                catch (IOException)
                {
                    // Sometimes files are still locked, wait a bit and try again
                    Thread.Sleep(100);
                    try
                    {
                        Directory.Delete(directoryPath, true);
                    }
                    catch
                    {
                        // Ignore cleanup failures in tests
                    }
                }
            }
        }

        /// <summary>
        /// Creates a test file with specified content
        /// </summary>
        /// <param name="directoryPath">Directory to create the file in</param>
        /// <param name="fileName">Name of the file</param>
        /// <param name="content">Content to write to the file</param>
        /// <returns>Full path to the created file</returns>
        public static string CreateTestFile(string directoryPath, string fileName, string content = "Test content")
        {
            var filePath = Path.Combine(directoryPath, fileName);
            File.WriteAllText(filePath, content);
            return filePath;
        }

        /// <summary>
        /// Waits for a condition to be true with timeout
        /// </summary>
        /// <param name="condition">Condition to wait for</param>
        /// <param name="timeoutMs">Timeout in milliseconds</param>
        /// <param name="intervalMs">Check interval in milliseconds</param>
        /// <returns>True if condition was met, false if timeout occurred</returns>
        public static bool WaitForCondition(Func<bool> condition, int timeoutMs = 5000, int intervalMs = 50)
        {
            var timeout = DateTime.Now.AddMilliseconds(timeoutMs);
            while (DateTime.Now < timeout)
            {
                if (condition())
                    return true;
                Thread.Sleep(intervalMs);
            }
            return false;
        }

        /// <summary>
        /// Generates a unique test file name
        /// </summary>
        /// <param name="extension">File extension (with or without dot)</param>
        /// <returns>Unique file name</returns>
        public static string GenerateUniqueFileName(string extension = ".txt")
        {
            extension = extension.StartsWith(".") ? extension : "." + extension;
            return $"test_file_{Guid.NewGuid()}{extension}";
        }

        /// <summary>
        /// Creates multiple test files for bulk testing
        /// </summary>
        /// <param name="directoryPath">Directory to create files in</param>
        /// <param name="count">Number of files to create</param>
        /// <param name="prefix">Prefix for file names</param>
        /// <returns>Array of created file paths</returns>
        public static string[] CreateMultipleTestFiles(string directoryPath, int count, string prefix = "bulk_test")
        {
            var files = new string[count];
            for (int i = 0; i < count; i++)
            {
                var fileName = $"{prefix}_{i:D3}.txt";
                files[i] = CreateTestFile(directoryPath, fileName, $"Content for file {i}");
            }
            return files;
        }

        /// <summary>
        /// Validates that an email address format is correct
        /// </summary>
        /// <param name="email">Email address to validate</param>
        /// <returns>True if valid format</returns>
        public static bool IsValidEmailFormat(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Encodes a string to Base64
        /// </summary>
        /// <param name="plainText">Plain text to encode</param>
        /// <returns>Base64 encoded string</returns>
        public static string EncodeBase64(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;

            var bytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// Decodes a Base64 string
        /// </summary>
        /// <param name="base64Text">Base64 encoded text</param>
        /// <returns>Decoded plain text</returns>
        public static string DecodeBase64(string base64Text)
        {
            if (string.IsNullOrEmpty(base64Text))
                return base64Text;

            try
            {
                var bytes = Convert.FromBase64String(base64Text);
                return System.Text.Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                return base64Text; // Return original if not valid Base64
            }
        }

        /// <summary>
        /// Asserts that an action completes within a specified time
        /// </summary>
        /// <param name="action">Action to execute</param>
        /// <param name="maxDurationMs">Maximum allowed duration in milliseconds</param>
        public static void AssertExecutionTime(Action action, int maxDurationMs)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            action();
            stopwatch.Stop();
            
            Assert.IsTrue(stopwatch.ElapsedMilliseconds <= maxDurationMs,
                         $"Action took {stopwatch.ElapsedMilliseconds}ms, expected <= {maxDurationMs}ms");
        }

        /// <summary>
        /// Creates a mock configuration for testing
        /// </summary>
        /// <returns>Path to temporary config file</returns>
        public static string CreateMockConfigFile()
        {
            var configPath = Path.GetTempFileName();
            var configContent = @"<?xml version='1.0' encoding='utf-8'?>
<configuration>
  <appSettings>
    <add key='SmtpServer' value='localhost' />
    <add key='SmtpPort' value='1025' />
    <add key='SmtpEnableSsl' value='false' />
    <add key='FromEmail' value='test@foldermonitor.local' />
    <add key='ToEmail' value='admin@test.local' />
    <add key='Environment' value='Development' />
  </appSettings>
</configuration>";
            File.WriteAllText(configPath, configContent);
            return configPath;
        }
    }

    /// <summary>
    /// Custom assertions for testing
    /// </summary>
    public static class CustomAsserts
    {
        /// <summary>
        /// Asserts that a file exists within a timeout period
        /// </summary>
        /// <param name="filePath">Path to the file</param>
        /// <param name="timeoutMs">Timeout in milliseconds</param>
        public static void FileExists(string filePath, int timeoutMs = 5000)
        {
            var exists = TestUtilities.WaitForCondition(() => File.Exists(filePath), timeoutMs);
            Assert.IsTrue(exists, $"File should exist: {filePath}");
        }

        /// <summary>
        /// Asserts that a directory exists within a timeout period
        /// </summary>
        /// <param name="directoryPath">Path to the directory</param>
        /// <param name="timeoutMs">Timeout in milliseconds</param>
        public static void DirectoryExists(string directoryPath, int timeoutMs = 5000)
        {
            var exists = TestUtilities.WaitForCondition(() => Directory.Exists(directoryPath), timeoutMs);
            Assert.IsTrue(exists, $"Directory should exist: {directoryPath}");
        }

        /// <summary>
        /// Asserts that a condition becomes true within a timeout period
        /// </summary>
        /// <param name="condition">Condition to check</param>
        /// <param name="message">Assertion message</param>
        /// <param name="timeoutMs">Timeout in milliseconds</param>
        public static void Eventually(Func<bool> condition, string message, int timeoutMs = 5000)
        {
            var conditionMet = TestUtilities.WaitForCondition(condition, timeoutMs);
            Assert.IsTrue(conditionMet, message);
        }

        /// <summary>
        /// Asserts that an action throws a specific exception type
        /// </summary>
        /// <typeparam name="T">Expected exception type</typeparam>
        /// <param name="action">Action to execute</param>
        /// <returns>The thrown exception</returns>
        public static T Throws<T>(Action action) where T : Exception
        {
            try
            {
                action();
                Assert.Fail($"Expected exception of type {typeof(T).Name} but no exception was thrown");
                return null; // Never reached
            }
            catch (T ex)
            {
                return ex;
            }
            catch (Exception ex)
            {
                Assert.Fail($"Expected exception of type {typeof(T).Name} but got {ex.GetType().Name}: {ex.Message}");
                return null; // Never reached
            }
        }
    }
}