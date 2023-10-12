namespace lilsync.Tests
{
    using System;
    using System.IO;
    using Xunit;
    using lilsync;
    
    public class SynchronizationTests : IDisposable
    {
        private readonly string _sourceFolder;
        private readonly string _replicaFolder;
        private readonly string _logFilePath;
    
        public SynchronizationTests()
        {
            // Set up test folders and files
            _sourceFolder = Path.Combine(Directory.GetCurrentDirectory(), "TestSourceFolder");
            _replicaFolder = Path.Combine(Directory.GetCurrentDirectory(), "TestReplicaFolder");
            _logFilePath = Path.Combine(Directory.GetCurrentDirectory(), "TestLog.txt");

            Console.WriteLine("Before folder creation check");
    
            // Create test folders
            Directory.CreateDirectory(_sourceFolder);
            Directory.CreateDirectory(_replicaFolder);
    
            // Create or clear the log file
            File.WriteAllText(_logFilePath, string.Empty);

            Console.WriteLine("After folders and files creation check");
        }
    
        [Fact]
        public void SyncBetweenEmptyFolders_ShouldSucceed()
        {
            // Arrange: Empty source and replica folders
    
            // Act: Perform synchronization
    
            Program.SynchronizeFolders(_sourceFolder, _replicaFolder, _logFilePath);
            
            // Assert: Verify that synchronization is successful (e.g., compare folder contents)
            Assert.Empty(Directory.GetFiles(_sourceFolder));
            Assert.Empty(Directory.GetFiles(_replicaFolder));
        }

        [Fact]
        public void FileDeletedInSource_ShouldDeleteInReplica()
        {
            // Arrange: Create a file in both source and replica
            string sourceFilePath = Path.Combine(_sourceFolder, "TestFile.txt");
            string replicaFilePath = Path.Combine(_replicaFolder, "TestFile.txt");
            File.WriteAllText(sourceFilePath, "Original content");
            File.WriteAllText(replicaFilePath, "Original content");

            // Act: Delete the file in the source
            File.Delete(sourceFilePath);
            Program.SynchronizeFolders(_sourceFolder, _replicaFolder, _logFilePath);

            // Assert: Verify that the file is deleted in the replica
            Assert.False(File.Exists(replicaFilePath));
        }
    
        [Fact]
        public void SyncWithFileModification_ShouldUpdateReplicaFile()
        {
            // Arrange: Create a file in source folder
            string sourceFilePath = Path.Combine(_sourceFolder, "TestFile.txt");
            File.WriteAllText(sourceFilePath, "Original content");
    
            // Act: Perform synchronization
            Program.SynchronizeFolders(_sourceFolder, _replicaFolder, _logFilePath);
    
            // Assert: Verify that the replica file is updated
            string replicaFilePath = Path.Combine(_replicaFolder, "TestFile.txt");
            string sourceContent = File.ReadAllText(sourceFilePath);
            string replicaContent = File.ReadAllText(replicaFilePath);
            Assert.Equal(sourceContent, replicaContent);
        }

        [Fact]
        public void FileRenamedInSource_ShouldRenameInReplica()
        {
            // Arrange: Create a file in the source and replica
            string sourceFilePath = Path.Combine(_sourceFolder, "TestFile.txt");
            string replicaFilePath = Path.Combine(_replicaFolder, "TestFile.txt");
            File.WriteAllText(sourceFilePath, "Original content");
            File.WriteAllText(replicaFilePath, "Original content");

            // Act: Rename the file in the source
            string newSourceFilePath = Path.Combine(_sourceFolder, "RenamedFile.txt");
            File.Move(sourceFilePath, newSourceFilePath);
            Program.SynchronizeFolders(_sourceFolder, _replicaFolder, _logFilePath);

            // Assert: Verify that the file is renamed in the replica
            string newReplicaFilePath = Path.Combine(_replicaFolder, "RenamedFile.txt");
            Assert.True(File.Exists(newReplicaFilePath));
            Assert.False(File.Exists(replicaFilePath));
        }

        [Fact]
        public void OrphanedFileInReplica_ShouldBeDeleted()
        {
            // Arrange: Create a file in the replica only
            string replicaFilePath = Path.Combine(_replicaFolder, "OrphanedFile.txt");
            File.WriteAllText(replicaFilePath, "Original content");

            // Act: Perform synchronization
            Program.SynchronizeFolders(_sourceFolder, _replicaFolder, _logFilePath);

            // Assert: Verify that the orphaned file is deleted in the replica
            Assert.False(File.Exists(replicaFilePath));
        }

        [Fact]
        public void UnchangedFileInSource_ShouldNotBeDeletedInReplica()
        {
            // Arrange: Create an unchanged file in both source and replica
            string sourceFilePath = Path.Combine(_sourceFolder, "UnchangedFile.txt");
            string replicaFilePath = Path.Combine(_replicaFolder, "UnchangedFile.txt");
            File.WriteAllText(sourceFilePath, "Original content");
            File.WriteAllText(replicaFilePath, "Original content");

            // Act: Perform synchronization
            Program.SynchronizeFolders(_sourceFolder, _replicaFolder, _logFilePath);

            // Assert: Verify that the file is still present in the replica
            Assert.True(File.Exists(replicaFilePath));
        }

        [Fact]
        public void FileModifiedInSource_ShouldUpdateInReplica()
        {
            // Arrange: Create a file in both source and replica
            string sourceFilePath = Path.Combine(_sourceFolder, "TestFile.txt");
            string replicaFilePath = Path.Combine(_replicaFolder, "TestFile.txt");
            File.WriteAllText(sourceFilePath, "Original content");
            File.WriteAllText(replicaFilePath, "Original content");

            // Act: Modify the file in the source
            File.WriteAllText(sourceFilePath, "Modified content");
            Program.SynchronizeFolders(_sourceFolder, _replicaFolder, _logFilePath);

            // Assert: Verify that the file is updated in the replica
            string sourceContent = File.ReadAllText(sourceFilePath);
            string replicaContent = File.ReadAllText(replicaFilePath);
            Assert.Equal(sourceContent, replicaContent);
        }

        [Fact]
        public void UnchangedFileInSource_ShouldNotBeModifiedInReplica()
        {
            // Arrange: Create an unchanged file in both source and replica
            string sourceFilePath = Path.Combine(_sourceFolder, "UnchangedFile.txt");
            string replicaFilePath = Path.Combine(_replicaFolder, "UnchangedFile.txt");
            File.WriteAllText(sourceFilePath, "Original content");
            File.WriteAllText(replicaFilePath, "Original content");

            // Act: Perform synchronization
            Program.SynchronizeFolders(_sourceFolder, _replicaFolder, _logFilePath);

            // Assert: Verify that the file is still identical in the replica
            string sourceContent = File.ReadAllText(sourceFilePath);
            string replicaContent = File.ReadAllText(replicaFilePath);
            Assert.Equal(sourceContent, replicaContent);
        }

        [Fact]
        public void OrphanedFoldersInReplica_ShouldBeDeleted()
        {
            // Arrange: Create a nested folder structure with orphaned folders in replica
            string sourceFolderPath = Path.Combine(_sourceFolder, "NestedFolder");
            string replicaFolderPath = Path.Combine(_replicaFolder, "NestedFolder");

            Directory.CreateDirectory(sourceFolderPath);
            Directory.CreateDirectory(replicaFolderPath);

            string orphanedFolder = Path.Combine(replicaFolderPath, "OrphanedFolder");
            Directory.CreateDirectory(orphanedFolder);

            // Act: Perform synchronization
            Program.SynchronizeFolders(_sourceFolder, _replicaFolder, _logFilePath);

            // Assert: Verify that orphaned folders in the replica are deleted
            Assert.False(Directory.Exists(orphanedFolder));
        }

        [Fact]
        public void MatchingNestedFolders_ShouldNotBeDeleted()
        {
            // Arrange: Create a nested folder structure without orphaned folders
            string sourceFolderPath = Path.Combine(_sourceFolder, "NestedFolder");
            string replicaFolderPath = Path.Combine(_replicaFolder, "NestedFolder");

            Directory.CreateDirectory(sourceFolderPath);
            Directory.CreateDirectory(replicaFolderPath);

            // Act: Perform synchronization
            Program.SynchronizeFolders(_sourceFolder, _replicaFolder, _logFilePath);

            // Assert: Verify that nested folders with matching structure are not deleted
            Assert.True(Directory.Exists(replicaFolderPath));
        }
        public void Dispose()
        {
            // Clean up test folders and files
            Directory.Delete(_sourceFolder, true);
            Directory.Delete(_replicaFolder, true);
            File.Delete(_logFilePath);
        }
    }

}
