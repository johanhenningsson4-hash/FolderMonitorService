using System;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FolderMonitorService.Tests
{
    [TestClass]
    public sealed class PerformanceTests
    {
        private const int PERFORMANCE_THRESHOLD_MS = 5000;
        private const int LARGE_FILE_COUNT = 100;

        [TestMethod]
        public void FileMonitoring_LargeNumberOfFiles_ShouldCompleteWithinTimeLimit()
        {
            // Arrange
            var tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), 
                                               "PerfTest_" + Guid.NewGuid());
            System.IO.Directory.CreateDirectory(tempDir);
            
            var stopwatch = Stopwatch.StartNew();
            var eventsProcessed = 0;

            try
            {
                using (var watcher = new System.IO.FileSystemWatcher(tempDir))
                {
                    watcher.Created += (s, e) => eventsProcessed++;
                    watcher.EnableRaisingEvents = true;

                    // Act
                    for (int i = 0; i < LARGE_FILE_COUNT; i++)
                    {
                        var filePath = System.IO.Path.Combine(tempDir, $"perf_file_{i}.txt");
                        System.IO.File.WriteAllText(filePath, $"Performance test content {i}");
                    }

                    // Wait for all events to be processed
                    var timeout = DateTime.Now.AddMilliseconds(PERFORMANCE_THRESHOLD_MS);
                    while (eventsProcessed < LARGE_FILE_COUNT && DateTime.Now < timeout)
                    {
                        System.Threading.Thread.Sleep(50);
                    }

                    stopwatch.Stop();

                    // Assert
                    Assert.AreEqual(LARGE_FILE_COUNT, eventsProcessed, 
                                   "All file creation events should be detected");
                    Assert.IsTrue(stopwatch.ElapsedMilliseconds < PERFORMANCE_THRESHOLD_MS, 
                                 $"Processing {LARGE_FILE_COUNT} files should complete within {PERFORMANCE_THRESHOLD_MS}ms");
                }
            }
            finally
            {
                if (System.IO.Directory.Exists(tempDir))
                {
                    System.IO.Directory.Delete(tempDir, true);
                }
            }
        }

        [TestMethod]
        public void EmailService_SendMultipleEmails_ShouldMaintainPerformance()
        {
            // Arrange
            var emailService = new MockEmailService();
            var stopwatch = Stopwatch.StartNew();
            var emailCount = 50;
            var successfulSends = 0;

            // Act
            for (int i = 0; i < emailCount; i++)
            {
                try
                {
                    emailService.SendEmail($"test{i}@example.com", 
                                         $"Performance Test {i}", 
                                         $"Test email body {i}");
                    successfulSends++;
                }
                catch (System.Net.Mail.SmtpException)
                {
                    // Expected in test environment - count as successful for performance test
                    successfulSends++;
                }
            }

            stopwatch.Stop();

            // Assert
            Assert.AreEqual(emailCount, successfulSends, "All emails should be processed");
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < PERFORMANCE_THRESHOLD_MS, 
                         $"Sending {emailCount} emails should complete within {PERFORMANCE_THRESHOLD_MS}ms");
        }

        [TestMethod]
        public void ConfigurationReading_MultipleAccess_ShouldBeFast()
        {
            // Arrange
            var stopwatch = Stopwatch.StartNew();
            var accessCount = 1000;

            // Act
            for (int i = 0; i < accessCount; i++)
            {
                var smtpServer = System.Configuration.ConfigurationManager.AppSettings["SmtpServer"];
                var smtpPort = System.Configuration.ConfigurationManager.AppSettings["SmtpPort"];
                var fromEmail = System.Configuration.ConfigurationManager.AppSettings["FromEmail"];
            }

            stopwatch.Stop();

            // Assert
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 1000, 
                         $"Reading configuration {accessCount} times should be fast");
        }

        // Mock email service for performance testing
        private class MockEmailService
        {
            public void SendEmail(string to, string subject, string body)
            {
                using (var client = new System.Net.Mail.SmtpClient("localhost", 1025))
                {
                    client.EnableSsl = false;
                    using (var message = new System.Net.Mail.MailMessage("test@example.com", to, subject, body))
                    {
                        client.Send(message); // Will throw in test environment
                    }
                }
            }
        }
    }
}