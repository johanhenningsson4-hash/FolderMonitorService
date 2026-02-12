using System;
using System.IO;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FolderMonitorService.Tests
{
    [TestClass]
    public sealed class FolderMonitoringTests
    {
        private string _testDirectory;
        private FolderMonitor _folderMonitor;

        [TestInitialize]
        public void Setup()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), "FolderMonitoringTests_" + Guid.NewGuid());
            Directory.CreateDirectory(_testDirectory);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _folderMonitor?.Stop();
            _folderMonitor?.Dispose();
            
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }

        [TestMethod]
        public void FolderMonitor_Start_ShouldEnableMonitoring()
        {
            // Arrange
            _folderMonitor = new FolderMonitor(_testDirectory);

            // Act
            _folderMonitor.Start();

            // Assert
            Assert.IsTrue(_folderMonitor.IsMonitoring, "Folder monitoring should be enabled after start");
        }

        [TestMethod]
        public void FolderMonitor_Stop_ShouldDisableMonitoring()
        {
            // Arrange
            _folderMonitor = new FolderMonitor(_testDirectory);
            _folderMonitor.Start();

            // Act
            _folderMonitor.Stop();

            // Assert
            Assert.IsFalse(_folderMonitor.IsMonitoring, "Folder monitoring should be disabled after stop");
        }

        [TestMethod]
        public void FolderMonitor_WithInvalidPath_ShouldThrowDirectoryNotFoundException()
        {
            // Arrange
            var invalidPath = @"C:\NonExistentFolder_" + Guid.NewGuid();

            // Act & Assert
            try
            {
                _folderMonitor = new FolderMonitor(invalidPath);
                _folderMonitor.Start();
                Assert.Fail("Expected DirectoryNotFoundException was not thrown");
            }
            catch (DirectoryNotFoundException)
            {
                // Expected exception
                Assert.IsTrue(true);
            }
        }

        [TestMethod]
        public void FolderMonitor_FileCreated_ShouldTriggerEvent()
        {
            // Arrange
            var eventTriggered = false;
            var testFileName = "monitor_test.txt";
            var testFilePath = Path.Combine(_testDirectory, testFileName);

            _folderMonitor = new FolderMonitor(_testDirectory);
            _folderMonitor.FileCreated += (sender, e) => {
                if (e.FullPath.EndsWith(testFileName))
                    eventTriggered = true;
            };

            _folderMonitor.Start();

            // Act
            File.WriteAllText(testFilePath, "Test content for monitoring");
            Thread.Sleep(200); // Allow time for event processing

            // Assert
            Assert.IsTrue(eventTriggered, "FileCreated event should have been triggered");
        }

        [TestMethod]
        public void FolderMonitor_FileDeleted_ShouldTriggerEvent()
        {
            // Arrange
            var eventTriggered = false;
            var testFileName = "delete_test.txt";
            var testFilePath = Path.Combine(_testDirectory, testFileName);
            
            // Create file first
            File.WriteAllText(testFilePath, "Content to be deleted");

            _folderMonitor = new FolderMonitor(_testDirectory);
            _folderMonitor.FileDeleted += (sender, e) => {
                if (e.FullPath.EndsWith(testFileName))
                    eventTriggered = true;
            };

            _folderMonitor.Start();

            // Act
            File.Delete(testFilePath);
            Thread.Sleep(200); // Allow time for event processing

            // Assert
            Assert.IsTrue(eventTriggered, "FileDeleted event should have been triggered");
        }

        [TestMethod]
        public void FolderMonitor_FileChanged_ShouldTriggerEvent()
        {
            // Arrange
            var eventTriggered = false;
            var testFileName = "change_test.txt";
            var testFilePath = Path.Combine(_testDirectory, testFileName);
            
            // Create file first
            File.WriteAllText(testFilePath, "Initial content");
            Thread.Sleep(100); // Ensure file is created before starting monitor

            _folderMonitor = new FolderMonitor(_testDirectory);
            _folderMonitor.FileChanged += (sender, e) => {
                if (e.FullPath.EndsWith(testFileName))
                    eventTriggered = true;
            };

            _folderMonitor.Start();
            Thread.Sleep(100); // Allow monitor to initialize

            // Act
            File.WriteAllText(testFilePath, "Modified content");
            Thread.Sleep(300); // Allow time for event processing

            // Assert
            Assert.IsTrue(eventTriggered, "FileChanged event should have been triggered");
        }

        [TestMethod]
        public void FolderMonitor_MultipleFiles_ShouldTriggerMultipleEvents()
        {
            // Arrange
            var eventCount = 0;
            var expectedFileCount = 5;

            _folderMonitor = new FolderMonitor(_testDirectory);
            _folderMonitor.FileCreated += (sender, e) => {
                Interlocked.Increment(ref eventCount);
            };

            _folderMonitor.Start();

            // Act
            for (int i = 0; i < expectedFileCount; i++)
            {
                var fileName = $"multi_test_{i}.txt";
                var filePath = Path.Combine(_testDirectory, fileName);
                File.WriteAllText(filePath, $"Content for file {i}");
                Thread.Sleep(50); // Small delay between file creations
            }

            Thread.Sleep(500); // Allow time for all events to process

            // Assert
            Assert.AreEqual(expectedFileCount, eventCount, 
                $"Should have triggered {expectedFileCount} FileCreated events");
        }

        [TestMethod]
        public void FolderMonitor_Subdirectories_ShouldMonitorRecursively()
        {
            // Arrange
            var eventTriggered = false;
            var subDirectory = Path.Combine(_testDirectory, "SubFolder");
            Directory.CreateDirectory(subDirectory);
            
            var testFileName = "sub_test.txt";
            var testFilePath = Path.Combine(subDirectory, testFileName);

            _folderMonitor = new FolderMonitor(_testDirectory, includeSubdirectories: true);
            _folderMonitor.FileCreated += (sender, e) => {
                if (e.FullPath.EndsWith(testFileName))
                    eventTriggered = true;
            };

            _folderMonitor.Start();

            // Act
            File.WriteAllText(testFilePath, "Subdirectory test content");
            Thread.Sleep(200); // Allow time for event processing

            // Assert
            Assert.IsTrue(eventTriggered, "Should monitor files in subdirectories when enabled");
        }

        [TestMethod]
        public void FolderMonitor_FileFilters_ShouldOnlyMonitorSpecifiedTypes()
        {
            // Arrange
            var txtEventTriggered = false;
            var logEventTriggered = false;

            _folderMonitor = new FolderMonitor(_testDirectory, filter: "*.txt");
            _folderMonitor.FileCreated += (sender, e) => {
                if (e.FullPath.EndsWith(".txt"))
                    txtEventTriggered = true;
                else if (e.FullPath.EndsWith(".log"))
                    logEventTriggered = true;
            };

            _folderMonitor.Start();

            // Act
            File.WriteAllText(Path.Combine(_testDirectory, "test.txt"), "TXT file content");
            File.WriteAllText(Path.Combine(_testDirectory, "test.log"), "LOG file content");
            Thread.Sleep(200); // Allow time for event processing

            // Assert
            Assert.IsTrue(txtEventTriggered, "Should trigger event for .txt files");
            Assert.IsFalse(logEventTriggered, "Should NOT trigger event for .log files when filter is *.txt");
        }
    }

    // Mock FolderMonitor class for testing
    public class FolderMonitor : IDisposable
    {
        private readonly string _path;
        private readonly bool _includeSubdirectories;
        private readonly string _filter;
        private System.IO.FileSystemWatcher _watcher;
        private bool _disposed = false;

        public bool IsMonitoring => _watcher?.EnableRaisingEvents ?? false;

        public event EventHandler<System.IO.FileSystemEventArgs> FileCreated;
        public event EventHandler<System.IO.FileSystemEventArgs> FileChanged;
        public event EventHandler<System.IO.FileSystemEventArgs> FileDeleted;

        public FolderMonitor(string path, bool includeSubdirectories = false, string filter = "*.*")
        {
            _path = path ?? throw new ArgumentNullException(nameof(path));
            _includeSubdirectories = includeSubdirectories;
            _filter = filter ?? "*.*";

            if (!Directory.Exists(_path))
                throw new DirectoryNotFoundException($"Directory not found: {_path}");
        }

        public void Start()
        {
            if (_watcher != null)
                return;

            _watcher = new System.IO.FileSystemWatcher(_path, _filter)
            {
                IncludeSubdirectories = _includeSubdirectories,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime
            };

            _watcher.Created += (s, e) => FileCreated?.Invoke(this, e);
            _watcher.Changed += (s, e) => FileChanged?.Invoke(this, e);
            _watcher.Deleted += (s, e) => FileDeleted?.Invoke(this, e);

            _watcher.EnableRaisingEvents = true;
        }

        public void Stop()
        {
            if (_watcher != null)
            {
                _watcher.EnableRaisingEvents = false;
                _watcher.Dispose();
                _watcher = null;
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Stop();
                _disposed = true;
            }
        }
    }
}