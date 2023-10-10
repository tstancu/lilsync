using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Reflection;


namespace lilsync 
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 4 && !args.Contains("--cleanup"))
            {
                Console.WriteLine("Usage: lilSync <sourceFolder> <replicaFolder> <logFilePath> <syncIntervalInSeconds>");
                return;
            }

            var sourceFolder = args[0];
            var replicaFolder = args[1];
            var logFilePath = args[2];
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
                Cleanup(sourceFolder, replicaFolder, logFilePath);
                Console.WriteLine("Cleanup completed successfully.");
            }
            else
            {
                try
                {
                    SynchronizeFolders(sourceFolder, replicaFolder, logFilePath, 2);
                    Console.WriteLine("Synchronization completed successfully.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred: {ex.Message}");
                }
            }
        }

        static void SynchronizeFolders(string sourceFolder, string replicaFolder, string logFilePath, int syncIntervalInSeconds)
        {
            string[] sourceFiles = Directory.GetFiles(sourceFolder, "*", SearchOption.AllDirectories);
            string[] replicaFiles = Directory.GetFiles(replicaFolder, "*", SearchOption.AllDirectories);

            Logger logger = Logger.Instance(logFilePath);

            HashSet<string> synchronizedReplicaFiles = new HashSet<string>();

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



            // check and delete files in the replica that are not present in the source folder

            foreach (string replicaFile in replicaFiles)
            {
                if (!synchronizedReplicaFiles.Contains(replicaFile))
                {
                    try
                    {
                        File.Delete(replicaFile);
                        logger.Log($"Deleted: {replicaFile.Substring(replicaFolder.Length)}");

                    }
                    catch (Exception ex)
                    {
                        logger.Log($"Error deleting {replicaFile}: {ex.Message}");
                    }
                }
            }

            static bool IsFileModified(string sourceFilePath, string replicaFilePath)
            {
                string sourceChecksum = ChecksumCalculator.CalculateMD5Checksum(sourceFilePath);
                string replicaChecksum = ChecksumCalculator.CalculateMD5Checksum(replicaFilePath);

                return sourceChecksum != replicaChecksum;
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

            // Cleanup build artifacts

            var assemblyLocation = Assembly.GetExecutingAssembly()?.Location;

            if (assemblyLocation != null)
            {
                var programFolder = Path.GetDirectoryName(assemblyLocation);

                #pragma warning disable CS8604
                var binFolder = Path.Combine(programFolder, "../../../bin");
                var objFolder = Path.Combine(programFolder, "../../../obj");
                #pragma warning restore CS8604

                if (Directory.Exists(binFolder))
                {
                    Directory.Delete(binFolder, true);
                    Console.WriteLine($"Bin folder deleted");
                }
                else
                {
                    Console.WriteLine($"{binFolder} does not exist.");
                }

                if (Directory.Exists(objFolder))
                {
                    Directory.Delete(objFolder, true);
                    Console.WriteLine($"Obj folder deleted");
                }
                else
                {
                    Console.WriteLine($"{objFolder} does not exist.");
                }
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

    public static class ChecksumCalculator
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