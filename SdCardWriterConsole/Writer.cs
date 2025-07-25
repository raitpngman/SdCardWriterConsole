using System.Collections.Concurrent;
using System.IO.Hashing;
using System.Runtime.InteropServices;
//SPDX-FileCopyrightText: © 2025 Sam Smucker <raitpngman@yahoo.com> 
//SPDX-License-Identifier: BSD-3-Clause

namespace SDCardCreatorConsole;

/// <summary>
/// The Writer class provides functionalities for managing and performing tasks on drives,
/// including file cleaning, file copying, drive validation, and ejection operations.
/// </summary>
internal class Writer
{
    /// <summary>
    /// Provides methods for handling drive-related operations such as cleaning up files, copying files, validating integrity,
    /// setting volume labels, and ejecting drives.
    /// </summary>
    internal Writer(Options options)
    {
        Option = options;

        for (var i = 0; i < options.Drives.Length; i++) Success.TryAdd(i, true);
    }

    /// <summary>
    /// Represents the collection of tasks or operations to be performed as part of the file management
    /// and processing workflow, such as file copying, validation, and drive operations.
    /// </summary>
    private ConcurrentBag<SourceWork> WorkToBeDone { get; } = new();


    /// <summary>
    /// Stores a collection of error messages encountered during the execution of various drive operations,
    /// such as erasing drives, setting volume labels, validation, or ejection processes.
    /// This property is used to log and track issues for further review or debugging.
    /// </summary>
    public List<string> ErrorMessages { get; set; } = new();

    /// <summary>
    /// Stores the instance of the configuration options for the operations, including
    /// cleaning type, validation behavior, volume label settings, and other operational parameters.
    /// </summary>
    private Options Option { get; }

    /// <summary>
    /// A concurrent dictionary that tracks the success or failure status of operations performed
    /// on individual drives, where the key represents the drive's index and the value indicates
    /// whether the operation was successful.
    /// </summary>
    private ConcurrentDictionary<int, bool> Success { get; set; } = new();

    /// <summary>
    /// Prepares and populates the collection of work items to be processed based on input folders.
    /// </summary>
    private void CreateWorkToDo()
    {
        if (WorkToBeDone.Any()) return;
        var tasks = new List<Task>();
        foreach (var inputFolder in Option.InputFolder)
        {
            var folder = inputFolder;
            var allFiles = Directory.GetFiles(inputFolder, "*.*", SearchOption.AllDirectories);
            foreach (var file in allFiles)
            {
                var t = Task.Run(() =>
                {
                    var hash = GetHash(file);
                    WorkToBeDone.Add(new SourceWork(file, folder, hash));
                });
                tasks.Add(t);
            }
        }

        Task.WaitAll(tasks);
    }

    /// <summary>
    /// Cleans files on the specified drives based on the configured cleaning type.
    /// </summary>
    /// <param name="progress">A progress reporter to track the progress of the cleaning operation.</param>
    /// <returns>A task representing the asynchronous cleaning operation.</returns>
    internal void CleanFilesAsync()
    {
        switch (Option.CleaningType)
        {
            case Cleaning.None:
                break;
            case Cleaning.Erase:
               List<Task> eraseTasks = new();
                    for (var i = 0; i < Option.Drives.Length; i++)
                    {
                        var x = i;
                        var e = EraseDrive(Option.Drives[x], x);
                        eraseTasks.Add(e);
                    }

                    Task.WaitAll(eraseTasks);
                
                break;
            // case Cleaning.Format:
            //     await Task.Run(async () =>
            //     {
            //         for (var i = 0; i < Option.Drives.Length; i++)
            //         {
            //             var x = i;
            //             await FormatDrive_CommandLine(Option.Drives[x], x);
            //         }
            //     });
            //     break;
            default:
                throw new Exception("Error cleaning drive. The option selected is not yet set up.");
        }
    }

    /// <summary>
    /// Erases the contents of the specified drive by deleting all files and directories.
    /// </summary>
    /// <param name="drive">The drive path to be erased.</param>
    /// <param name="position">The position of the drive in the array of drives.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    internal async Task EraseDrive(string drive, int position)
    {
        await Task.Run(() =>
        {
            try
            {
                var di = new DirectoryInfo(drive);
                var diInfo = di.GetDirectories();
                foreach (var item in diInfo) item.Delete(true);

                var files = di.GetFiles("*", SearchOption.TopDirectoryOnly);
                foreach (var file in files) file.Delete();
            }
            catch (Exception ex)
            {
                ErrorMessages.Add($"Error in erasing drive {drive} ({ex.Message})");
                Success[position] = false;
            }
        });
    }

    /*
     //Removed due to windows only code and also need for administrative properties.
    /// <summary>
    ///     This requires admin privileges on a program level (app manifest) to work.
    /// </summary>
    /// <param name="driveLetter"></param>
    /// <param name="position"></param>
    /// <param name="label"></param>
    /// <param name="fileSystem"></param>
    /// <param name="quickFormat"></param>
    /// <param name="enableCompression"></param>
    /// <param name="clusterSize"></param>
    /// <returns></returns>
    private async Task FormatDrive_CommandLine(string driveLetter, int position,
        string label = "", string fileSystem = "FAT32", bool quickFormat = true,
        bool enableCompression = false, int? clusterSize = 4096)
    {
        await Task.Run(async () =>
        {
            //		enum FormatResult
            //{
            //	Success = 0,
            //	UnsupportedFileSystem = 1,
            //	IncompatibleMediaInDrive = 2,
            //	AccessDenied = 3,
            //	CallCanceled = 4,
            //  ...
            //  UnknownError = 18
            //}

            var driveLetterCleaned = driveLetter.Replace("\\", "");

            try
            {
                //query and format given drive
                var searcher = new ManagementObjectSearcher
                    (@"select * from Win32_Volume WHERE DriveLetter = '" + driveLetterCleaned + "'");
                foreach (ManagementObject vi in searcher.Get())
                {
                    var resultcode = vi.InvokeMethod("Format", new object[]
                        { fileSystem, quickFormat, clusterSize, label, enableCompression });
                    //TODO: Process resultcode.
                }
            }

            catch (Exception ex)
            {
                ErrorMessages.Add($"Error formatting drive {driveLetter} ({ex.Message})");
                Success[position] = false;
            }
        }).ConfigureAwait(false);
    }
    */

    /// <summary>
    /// Asynchronously performs the operation of copying files, ensuring the required tasks are executed and progress is tracked.
    /// </summary>
    /// <param name="progress">An interface for reporting progress updates during the file copying operation.</param>
    internal async Task CopyFilesAsync(IProgress<double> progress)
    {
        CreateWorkToDo();
 
        //var allFiles = Directory.GetFiles(inputFolder, "*.*", SearchOption.AllDirectories);
        var totalRecords = WorkToBeDone.Count;
        var bufferSize = 1024 * 256;
        var counter = 1;
        foreach (var work in WorkToBeDone)
        {
            var fileTasks = new List<Task>();
            progress.Report(counter / (double)totalRecords);
            counter++;

            var d = new string[Option.Drives.Length];


            for (var i = 0; i < d.Length; i++)
            {
                var ii = i;
                var t = Task.Run(() =>
                {
                    var ff = work.GetNewFile(Option.Drives[ii]);

                    if (ShouldWrite(ff, work.Hash, ii)) d[ii] = ff;
                });
                fileTasks.Add(t);
            }

            Task.WaitAll(fileTasks.ToArray());


            var hasWork = false;
            foreach (var destinationPath in d)
                if (!string.IsNullOrEmpty(destinationPath))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(destinationPath) ?? string.Empty);
                    hasWork = true;
                }

            if (!hasWork) continue;


            // Set up the source and outputs
            using (var source = new FileStream(work.File, FileMode.Open, FileAccess.Read, FileShare.Read,
                       bufferSize,
                       FileOptions.SequentialScan))

            {
                var outputs = d.Where(x => !string.IsNullOrEmpty(x)).Select(p =>
                    new FileStream(p, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize)).ToArray();
                if (outputs.Length == 0) continue;
                // Set up the copy operation

                var buffer = new byte[bufferSize];
                int read;
                while ((read = await source.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    foreach (var outputStream in outputs) fileTasks.Add(outputStream.WriteAsync(buffer, 0, read));

                    Task.WaitAll(fileTasks.ToArray());
                }

                foreach (var fileStream in outputs)
                {
                    var t = Task.Run(() =>
                    {
                        fileStream.Flush();
                        fileStream.Dispose();
                    });
                    fileTasks.Add(t);
                }

                Task.WaitAll(fileTasks.ToArray());
            }
        }
    }


    /// <summary>
    /// Determines whether a file should be written based on its existence, integrity, and the specified overwrite options.
    /// </summary>
    /// <param name="newFilePath">The path to the new file that is being evaluated for writing.</param>
    /// <param name="originalHash">The hash of the original file, used for comparison when validating file integrity.</param>
    /// <param name="position">The position of the drive in the operation, used to track success or failure states.</param>
    /// <returns>
    /// A boolean value indicating whether the file should be written.
    /// Returns true if the file needs to be written; otherwise, returns false.
    /// </returns>
    private bool ShouldWrite(string newFilePath, string originalHash, int position)
    {
        //Don't even bother trying if there has been some error.
        if (Success[position] == false) return false;

        //If the file exists, then check and see if it is okay. Only copy it if needed.
        switch (Option.OverWriteType)
        {
            case Overwrite.Never:
                if (File.Exists(newFilePath)) return false;

                break;
            case Overwrite.Different:
                if (AreEqual(newFilePath, originalHash)) return false;

                break;
            case Overwrite.Always:
                break;

            default:
                throw new ArgumentException("The options for overwrite does not exist");
        }

        return true;
    }


    /// <summary>
    /// Sets the volume label for the specified drives using the given label name.
    /// Supports operations for Windows and Linux platforms.
    /// If the operation fails, an error message is added to the error list.
    /// </summary>
    /// <param name="newLabel">The new volume label to be assigned to the drives.</param>
    internal void SetVolumeLabel(string newLabel)
    {
        if (string.IsNullOrEmpty(newLabel)) return;

        ILabel labelWriter;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            labelWriter = new LabelWin();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            labelWriter = new LabelLinux();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            Console.WriteLine("OSX is not supported yet for ejecting drives.");
            return;
        }
        else
        {
            Console.WriteLine("Operating system is not identified. Unable to eject drive");
            return;
        }

        var success = labelWriter.WriteLabel(newLabel, Option.Drives);
        if (!success) ErrorMessages.Add("Error setting volume labels.");
    }

    /// <summary>
    /// Validates the copied files on the drive to ensure integrity and correctness of the operation.
    /// This process uses progress reporting to indicate the validation progress.
    /// </summary>
    /// <param name="progress">
    /// An <see cref="IProgress{T}"/> instance for reporting the validation progress as a percentage.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous validation operation.
    /// </returns>
    internal void ValidateAsync(IProgress<double> progress)
    {
        CreateWorkToDo();
        ValidateCopyAsync(progress);

    }

    /// <summary>
    /// Validates the copied files on all specified drives by comparing their hash values to ensure data integrity.
    /// Reports progress as each validation task completes.
    /// </summary>
    /// <param name="progress">An object that reports the progress of the validation as a percentage.</param>
    /// <returns>A task representing the asynchronous validation operation.</returns>
    private void ValidateCopyAsync(IProgress<double> progress)
    {
        var driveCount = Option.Drives.Length;
        var totalRecords = WorkToBeDone.Count;
        var countFinished = 0;
        var fileTasks = new List<Task>();
        foreach (var workA in WorkToBeDone)
        {
            var work = workA;
            var tOuter = Task.Run(() =>
            {
                for (var i = 0; i < driveCount; i++)
                {
                    var ii = i;

                    if (Success[ii])
                    {
                        var destFile = work.GetNewFile(Option.Drives[ii]);
                        var equal = AreEqual(destFile, work.Hash);
                        if (!equal)
                        {
                            Success[ii] = false;
                            ErrorMessages.Add($"Validation failed for drive {Option.Drives[ii]}.");
                        }
                    }
                }
                Interlocked.Increment(ref countFinished);
                progress.Report((double)countFinished / (double)totalRecords);
            });
            fileTasks.Add(tOuter);
        }

        Task.WaitAll(fileTasks);
        // while (fileTasks.Any())
        // {
        //     Task.Delay(2000).Wait();
        //     fileTasks.RemoveAll(x => x.IsCompleted);
        //     progress.Report((totalRecords - fileTasks.Count) / (double)totalRecords);
        // }
    }


    /// <summary>
    ///     This checks the hash and sees if they are equal.
    /// </summary>
    /// <param name="newFilePath"></param>
    /// <param name="originalHash"></param>
    /// <returns></returns>
    private bool AreEqual(string newFilePath, string originalHash)
    {
        if (!File.Exists(newFilePath)) return false;
        var hash = GetHash(newFilePath);
        return hash == originalHash;
    }

    /// <summary>
    /// Computes the hash of a specified file located at the given path using the XxHash64 algorithm.
    /// </summary>
    /// <param name="path">The file path from which the hash is computed. If the file does not exist, a unique hash is returned.</param>
    /// <returns>A string representing the computed hash of the file in hexadecimal format, or a unique hash if the file does not exist.</returns>
    private string GetHash(string path)
    {
        var hash = new XxHash64();
        if (Path.Exists(path))
        {
            using var stream = new BufferedStream(File.OpenRead(path), 1200000);
            hash.Append(stream);
            return BitConverter.ToString(hash.GetHashAndReset()).Replace("-", string.Empty);
        }

        //Return a unique hash that won't match anything.
        return Guid.NewGuid().ToString();
    }


    /// <summary>
    /// Ejects drives after validating and performing operations based on the configured options.
    /// Determines the operating system and uses the appropriate mechanism to unmount valid drives.
    /// Adds any errors encountered during the ejection process to the error messages list.
    /// </summary>
    internal void Eject()
    {
        if (!Option.Eject) return;

        IEject ejector;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            ejector = new UsbEjectWin();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            ejector = new UsbEjectLinux();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            Console.WriteLine("OSX is not supported yet for ejecting drives.");
            return;
        }
        else
        {
            Console.WriteLine("Operating system is not identified. Unable to eject drive");
            return;
        }

        var s = "";
        var drivesToUnmount = new List<string>();
        foreach (var key in Success.Keys)
            if (Success[key])
                drivesToUnmount.Add(Option.Drives[key]);
            else
                s += $", {Option.Drives[key]}";

        var success = ejector.Unmount(drivesToUnmount.ToArray());
        if (success == false) s = "Error ejecting drives.";
        if (!string.IsNullOrEmpty(s)) ErrorMessages.Add(s);
    }

    /// <summary>
    /// Represents a record containing details about a source work file, including its file path,
    /// the input folder it originates from, and its associated hash value.
    /// Provides functionality to generate a new file path based on the provided base path.
    /// </summary>
    private record SourceWork(string File, string InputFolder, string Hash)

    {
        public string GetNewFile(string newFileName)
        {
            return File.Replace(InputFolder, newFileName);
        }
    }
}