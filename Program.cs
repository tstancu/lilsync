using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;


namespace lilsync 
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 4)
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


                // replicaDirectory = Path.GetDirectoryName(replicaFile)!;

                // if (!Directory.Exists(replicaDirectory))
                // {
                //     Directory.CreateDirectory(replicaDirectory);
                //     LogAction(logFilePath, $"Created missing directory: {replicaDirectory}");
                // }

                // Check if the file exists in the replica

                if (File.Exists(replicaFile))
                {
                    FileInfo replicaFileInfo = new FileInfo(replicaFile);

                    if (sourceFileInfo.LastWriteTimeUtc > replicaFileInfo.LastWriteTimeUtc || sourceFileInfo.Length != replicaFileInfo.Length)
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
        }

        // static void LogAction(string logFilePath, string message)
        // {
        //     try 
        //     {
        //         // Console.WriteLine("logFilePath: " + logFilePath);

        //         string formattedMessage = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} - {message}";

        //         string logFileName = $"log_{DateTime.Now:yyyyMMddHHmmss}.txt";
        //         // string logFilePath = Path.Combine(logFilePath);

        //         Console.WriteLine($"Logged: {formattedMessage}");

        //         using (StreamWriter writer = new StreamWriter(logFilePath, true))
        //         {
        //             writer.WriteLine(formattedMessage);
        //         }
        //     }
        //     catch (Exception ex)
        //     {
        //         Console.WriteLine($"Error while logging: {ex.Message}");
        //     }
        // }



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

}