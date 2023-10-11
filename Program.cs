using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Reflection;
using System.Timers;


namespace lilsync 
{
    class Program
    {
        private static Timer? synchronizationTimer;
        private static string sourceFolder = null!;
        private static string replicaFolder = null!;
        private static string logFilePath = null!;



        static void Main(string[] args)
        {
            if (args.Length != 4 && !args.Contains("--cleanup"))
            {
                Console.WriteLine("Usage: lilSync <sourceFolder> <replicaFolder> <logFilePath> <syncIntervalInSeconds>");
                return;
            }

            sourceFolder = args[0];
            replicaFolder = args[1];
            logFilePath = args[2];
            var syncIntervalInSeconds = int.Parse(args[3]);

            if (!Directory.Exists(sourceFolder))
            {
                Console.WriteLine($"Source folder '{sourceFolder}' does not exist.");
                return;
            }

            if (!Directory.Exists(replicaFolder))
            {
                Console.WriteLine($"Replica folder '{replicaFolder}' does not exist.");
            }

            if (args.Contains("--cleanup"))
            {
                // Gracefully stop the synchronization timer, if it's running.
                synchronizationTimer?.Stop();
                synchronizationTimer?.Dispose();

                Cleanup(sourceFolder, replicaFolder, logFilePath);
                Console.WriteLine("Cleanup completed successfully.");
            }
            else
            {
                synchronizationTimer = new Timer(syncIntervalInSeconds * 1000);
                synchronizationTimer.Elapsed += new ElapsedEventHandler(SynchronizeFolderCallback);
                synchronizationTimer.AutoReset = true;
                synchronizationTimer.Enabled = true;

                System.Console.WriteLine($"Synchronization will occur every {syncIntervalInSeconds} seconds. Please Enter to exit.");
                Console.ReadLine();

                // // Gracefully stop the synchronization timer when the application exits.
                synchronizationTimer?.Stop();
                synchronizationTimer?.Dispose();
            }
        }

        static void SynchronizeFolders(string sourceFolder, string replicaFolder, string logFilePath)
        {
            string[] sourceFiles = Directory.GetFiles(sourceFolder, "*", SearchOption.AllDirectories);
            string[] replicaFiles = Directory.GetFiles(replicaFolder, "*", SearchOption.AllDirectories);
            string[] sourceDirectories = Directory.GetDirectories(sourceFolder, "*", SearchOption.AllDirectories);

            Logger logger = Logger.Instance(logFilePath);

            var synchronizedReplicaFiles = new HashSet<string>();
            var synchronizedDirectories = new HashSet<string>();
            

            foreach (string sourceFile in sourceFiles)
            {
                
                string relativePath = sourceFile.Substring(sourceFolder.Length);
                string replicaFile = Path.GetFullPath(replicaFolder + relativePath);
                FileInfo sourceFileInfo = new FileInfo(sourceFile);

                logger.Log($"Checking: {relativePath}");

                // Check if the file exists in the replica

                if (File.Exists(replicaFile))
                {
                    FileInfo replicaFileInfo = new FileInfo(replicaFile);

                    if (IsFileModified(sourceFile, replicaFile))
                    {
                        try 
                        {
                            // Copy the source file to the replica
                            File.Copy(sourceFile, replicaFile, true);
                            logger.Log($"Updated: {relativePath}");

                        }
                        catch (Exception ex)
                        {
                            logger.Log($"Error updating {relativePath}: {ex.Message}");
                        }
                    }
                }
                else // Replica file does not exist
                {
                    try
                    {
                        string replicaDirectory = Path.GetDirectoryName(replicaFile)!;

                        if (!Directory.Exists(replicaDirectory))
                        {
                            Directory.CreateDirectory(replicaDirectory);
                            logger.Log($"Created missing directory: {replicaDirectory}");
                        }

                        // Copy the source file to the replica

                        File.Copy(sourceFile, replicaFile);
                        logger.Log($"Created: {relativePath}");
                    }
                    catch (Exception ex)
                    {
                        logger.Log($"Error copying {relativePath}: {ex.Message}");
                    }
                }

                synchronizedReplicaFiles.Add(replicaFile);
            }

            foreach (string sourceDirectory in sourceDirectories)
            {
                string relativePath = sourceDirectory.Substring(sourceFolder.Length);
                string replicaDirectory = Path.GetFullPath(replicaFolder + relativePath);

                if (!Directory.Exists(replicaDirectory))
                {
                    Directory.CreateDirectory(replicaDirectory);
                    logger.Log($"Created missing directory: {relativePath}");
                }

                synchronizedDirectories.Add(replicaDirectory);
            }

            // check and delete files in the replica that are not present in the source folder

            foreach (string replicaFile in replicaFiles)
            {
                if (!synchronizedReplicaFiles.Contains(replicaFile))
                {
                    if (File.Exists(replicaFile))
                    {
                        try
                        {
                            File.Delete(replicaFile);
                            logger.Log($"Deleted: {replicaFile.Substring(replicaFolder.Length)}");

                            // Delete parent directories if no other files/directories are present
                            // string parentDirectory = Path.GetDirectoryName(replicaFile)!;
                            // while (!string.Equals(parentDirectory, replicaFolder, StringComparison.OrdinalIgnoreCase) && Directory.Exists(parentDirectory) && Directory.GetFileSystemEntries(parentDirectory).Length == 0)
                            // {
                            //     Directory.Delete(parentDirectory);
                            //     parentDirectory = Path.GetDirectoryName(parentDirectory)!;
                            // }

                        }
                        catch (Exception ex)
                        {
                            logger.Log($"Error deleting {replicaFile}: {ex.Message}");
                        }
                    }
                }
            }

            RemoveEmptyDirectories(replicaFolder, synchronizedDirectories, logger);

            static bool IsFileModified(string sourceFilePath, string replicaFilePath)
            {
                string sourceChecksum = Utils.CalculateMD5Checksum(sourceFilePath);
                string replicaChecksum = Utils.CalculateMD5Checksum(replicaFilePath);

                return sourceChecksum != replicaChecksum;
            }

            static void RemoveEmptyDirectories(string baseFolder, HashSet<string> synchronizedDirectories, Logger logger)
            {
                var allDirectories = Directory.GetDirectories(baseFolder, "*", SearchOption.AllDirectories).OrderByDescending(dir => dir.Length);

                foreach (string directory in allDirectories)
                {
                    if (!Directory.GetFileSystemEntries(directory).Any() && !synchronizedDirectories.Contains(directory))
                    {
                        try
                        {
                            Directory.Delete(directory);
                            logger.Log($"Deleted empty directory: {directory}");
                        }
                        catch (Exception ex)
                        {
                            logger.Log($"Error deleting directory {directory}: {ex.Message}");
                        }
                    }
                }
            }
        }

        private static void SynchronizeFolderCallback(object? sender, ElapsedEventArgs e)
        {
            try
            {
                SynchronizeFolders(sourceFolder, replicaFolder, logFilePath);
                System.Console.WriteLine("Synchronization completed successfully.");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }

        static void Cleanup(string sourceFolder, string replicaFolder, string logFilePath)
        {
            if (Directory.Exists(replicaFolder))
            {
                foreach (var file in Directory.GetFiles(replicaFolder))
                {
                    File.Delete(file);
                }
                foreach (var dir in Directory.GetDirectories(replicaFolder))
                {
                    Directory.Delete(dir, true);
                }

                Console.WriteLine($"Contents of {replicaFolder} have been deleted.");
            }
            else
            {
                Console.WriteLine($"{replicaFolder} does not exist.");
            }

            if (File.Exists(logFilePath))
            {
                string logDirectory = Path.GetDirectoryName(logFilePath)!;

                if (Directory.Exists(logDirectory))
                {
                    Directory.Delete(logDirectory, true);
                    Console.WriteLine($"Contents of {logDirectory} have been deleted.");
                }
                else
                {
                    Console.WriteLine($"{logDirectory} does not exist.");
                }
            }
            else 
            {
                Console.WriteLine($"{logFilePath} does not exist.");
            }
        }
    }

    public class Logger
    {
        private static Logger? instance = null;
        private readonly string logFilePath;
        private readonly object lockObject = new object();

        private Logger(string logFilePath)
        {
            this.logFilePath = logFilePath;
            InitializeLogFile();
        }

        public static Logger Instance(string logFilePath)
        {
            if (instance == null)
            {
                System.Console.WriteLine("logFilePath: " + logFilePath);
                instance = new Logger(logFilePath);
            }
            return instance;
        }

        public void InitializeLogFile()
        {

            string logsDirectory = Path.GetDirectoryName(logFilePath)!;

            if (!Directory.Exists(logsDirectory))
            {
                Directory.CreateDirectory(logsDirectory);
            }

            // create log file if it does not exist
            if (!File.Exists(logFilePath))
            {
                lock(lockObject)
                {
                    // using (FileStream fileStream = new FileStream(logFilePath, FileMode.Create, FileAccess.Write, FileShare.Read))
                    using (StreamWriter writer = File.CreateText(logFilePath))
                    {
                        // initialize the log file with headers or metadata
                        writer.WriteLine($"Log started at {DateTime.Now}");
                    }
                }
            }
        }

        public void Log(string message)
        {
            string formattedMessage = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} - {message}";

            lock (lockObject)
            {
                using (StreamWriter writer = File.AppendText(logFilePath))
                {
                    writer.WriteLine(formattedMessage);
                }
            }

            System.Console.WriteLine(formattedMessage);
        }
    }

    public static class Utils
    {
        public static string CalculateMD5Checksum(string filePath)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filePath))
                {
                    byte[] hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLower();
                }
            }
        }
    }

}