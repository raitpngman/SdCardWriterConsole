//SPDX-FileCopyrightText: © 2025 Sam Smucker <raitpngman@yahoo.com> 
//SPDX-License-Identifier: BSD-3-Clause

using System.Runtime.InteropServices;
using System.Text;

namespace SDCardCreatorConsole;

/// <summary>
/// Represents the core logic of the SD card creation process. Handles configuration,
/// execution of copy operations, drive validation, and error handling.
/// </summary>
/// <remarks>
/// This class is designed to be used in a console application for automating the process
/// of copying files to multiple drives, validating the copied data, and ejecting drives
/// as necessary. The workflow includes input folder selection, drive configuration,
/// and copy/validation operations.
/// </remarks>
public class CopyEngine
{
    private Options WritingOptions { get; } = new();

    public void Run()
    {
        GetOptions();
        while (true)
            try
            {
                GetDrivesToWriteTo();
                while (!ConfirmOptions())
                {
                    GetOptions();
                    GetDrivesToWriteTo();
                }

                var t = RunRound();
                t.Wait();
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "Error encountered. It is recommended to check the drives are connected and try again.");
                Console.WriteLine(ex);
            }
    }


    /// <summary>
    /// Executes the primary operations in a sequential manner, including cleaning files, copying files,
    /// validating files, and handling errors if any. This method manages the workflow and ensures the
    /// progress of operations is displayed to the user via console output.
    /// </summary>
    /// <returns>
    /// A Task representing the asynchronous operation of running through the workflows, such as file operations
    /// and outputting the corresponding progress or error messages.
    /// </returns>
    private async Task RunRound()
    {
        var write = new Writer(WritingOptions);
        if (!WritingOptions.OnlyValidate)
        {
            write.SetVolumeLabel(WritingOptions.VolumeLabel);

            Console.WriteLine("Cleaning files if needed");
            
             write.CleanFilesAsync();
            

            Console.WriteLine();
            Console.WriteLine($"{DateTime.Now} Copying files");
            var barCopy = new ProgressBar();
            var t = write.CopyFilesAsync(barCopy);
            t.Wait();
            barCopy.Dispose();
            Console.WriteLine("Copying finished");
        }

        Console.WriteLine();
        Console.WriteLine($"{DateTime.Now}	");
        if (WritingOptions.Validate || WritingOptions.OnlyValidate)
        {
            Console.WriteLine("Validating files");
            var barValidate = new ProgressBar();
            write.ValidateAsync(barValidate);
            barValidate.Dispose();
            Console.WriteLine("Validation finished");
            Console.WriteLine();
        }

        Console.WriteLine();
        write.Eject();
        Console.WriteLine();
        Console.WriteLine();
        if (write.ErrorMessages.Any())
        {
            var sb = new StringBuilder();
            foreach (var item in write.ErrorMessages) sb.AppendLine(item);

            Console.WriteLine(sb.ToString());
            Console.WriteLine($"{DateTime.Now} - Completed with errors. See above for details");

            return;
            //throw new Exception("Completed with errors. See above for details");
        }

        Console.WriteLine($"{DateTime.Now} - Completed successfully. No errors found");

        Console.WriteLine($"{DateTime.Now} ");
        Console.WriteLine("Press any key to continue");
        Console.ReadKey();
    }

    /// <summary>
    /// Prompts the user to confirm or modify the current configuration of options for the SD card creation process.
    /// Displays the current settings, such as input folders, validation, cleaning, overwrite preferences,
    /// and drive-related options. Provides the ability to either proceed, modify, or exit the process.
    /// </summary>
    /// <returns>
    /// A boolean value indicating whether the user has confirmed the current configuration.
    /// Returns true if the user decides to continue with the current options, or false if modifications are needed.
    /// </returns>
    private bool ConfirmOptions()
    {
        var shouldContinue = true;
        Console.WriteLine("Please confirm options");
        foreach (var o in WritingOptions.InputFolder) Console.WriteLine($"Input folder - {o}");

        Console.WriteLine($"Validate only = {WritingOptions.OnlyValidate}");
        if (!WritingOptions.OnlyValidate)
        {
            Console.WriteLine($"Volume label = {WritingOptions.VolumeLabel}");
            Console.WriteLine($"Cleaning type = {WritingOptions.CleaningType}");
            Console.WriteLine($"Overwrite type = {WritingOptions.OverWriteType}");
            Console.WriteLine($"Validate = {WritingOptions.Validate}");
        }

        Console.WriteLine($"Eject = {WritingOptions.Eject}");
        Console.WriteLine($"Number of drives to validate/copy to = {WritingOptions.Drives.Length}");
        Console.WriteLine();
        Console.WriteLine("Press 1 to change, q to quit, or any other key to continue");
        var key = Console.ReadKey();
        if (key.Key == ConsoleKey.Q)
            Environment.Exit(0);
        else if (key.Key == ConsoleKey.D1 || key.Key == ConsoleKey.NumPad1) shouldContinue = false;

        return shouldContinue;
    }


    /// <summary>
    /// Configures the options required for the SD card creation process.
    /// This method prompts the user with a series of inputs to specify folder locations,
    /// validation mode, volume label, cleaning preferences, overwrite settings, and drive ejection options.
    /// It manages the interactive setup of all configurations except for drive selection, which occurs later.
    /// </summary>
    private void GetOptions()
    {
        //Get the folder options
        Console.WriteLine("At anytime, press enter 'q' to quit");
        Console.WriteLine("Setting Options");
        SelectFolders();
        ChooseValidateOnly();
        if (!WritingOptions.OnlyValidate)
        {
            SetVolumeLabel();
            ChooseCleaning();
            ChooseOverwrite();
            ValidateDrives();
        }

        ChooseEject();
        //Don't do this one because it needs to be done for each round.
        //GetDrivesToWriteTo();
    }

    /// <summary>
    /// Prompts the user to select a file overwrite option for the copy process. The user can choose between
    /// never overwriting, always overwriting, or overwriting only if the file differs. This setting determines
    /// how files will be handled when conflicts arise during the copy operation.
    /// </summary>
    private void ChooseOverwrite()
    {
        //Never, always, If different

        while (true)
        {
            Console.WriteLine();
            Console.WriteLine("Enter your file overwrite option");
            Console.WriteLine("1 - never overwrite file");
            Console.WriteLine("2 - always overwrite file");
            Console.WriteLine("3 - overwrite if the file is different");
            var consoleInput = Console.ReadLine();
            if (consoleInput == "q")
            {
                Environment.Exit(0);
            }
            else if (consoleInput == "1")
            {
                WritingOptions.OverWriteType = Overwrite.Never;
                return;
            }
            else if (consoleInput == "2")
            {
                WritingOptions.OverWriteType = Overwrite.Always;
                return;
            }
            else if (consoleInput == "3")
            {
                WritingOptions.OverWriteType = Overwrite.Different;
                return;
            }
            else
            {
                Console.WriteLine("Invalid entry");
            }
        }
    }

    /// <summary>
    /// Allows the user to specify the cleaning option for drives before writing operations commence.
    /// The user can choose to either erase all items on the drives or proceed without erasing.
    /// This decision is captured and stored in the configuration for subsequent operations in the workflow.
    /// </summary>
    private void ChooseCleaning()
    {
        //none, erase

        while (true)
        {
            Console.WriteLine();
            Console.WriteLine("Enter your cleaning option");
            Console.WriteLine("1 - erase all items");
            Console.WriteLine("2 - not erase");

            var consoleInput = Console.ReadLine();
            if (consoleInput == "q")
            {
                Environment.Exit(0);
            }
            else if (consoleInput == "1")
            {
                WritingOptions.CleaningType = Cleaning.Erase;
                break;
            }
            else if (consoleInput == "2")
            {
                WritingOptions.CleaningType = Cleaning.None;
                break;
            }
            else
            {
                Console.WriteLine("Invalid entry");
            }
        }
    }

    /// <summary>
    /// Configures the user's preference for ejecting drives after the copying or validation process completes.
    /// Provides options for immediate ejection or skipping the ejection step and updates the settings
    /// accordingly based on user input.
    /// </summary>
    /// <remarks>
    /// This method is executed as part of the workflow to determine whether drives should be ejected
    /// automatically upon task completion. It continuously prompts for valid input, allowing the user
    /// to exit or select their choice, ensuring clarity in the configuration.
    /// </remarks>
    private void ChooseEject()
    {
        while (true)
        {
            Console.WriteLine();
            Console.WriteLine("Enter your ejection option");
            Console.WriteLine("1 - eject when finished");
            Console.WriteLine("2 - do not eject");
            var consoleInput = Console.ReadLine();
            if (consoleInput == "q")
            {
                Environment.Exit(0);
            }
            else if (consoleInput == "1")
            {
                WritingOptions.Eject = true;
                return;
            }
            else if (consoleInput == "2")
            {
                WritingOptions.Eject = false;
                return;
            }
            else
            {
                Console.WriteLine("Invalid entry");
            }
        }
    }


    /// <summary>
    /// Prompts the user to determine whether files should be copied or only validated.
    /// Updates the application settings based on the user's input. Provides options for
    /// copying files, validating files, or quitting the application.
    /// </summary>
    /// <remarks>
    /// This method ensures that the user explicitly chooses between file copying or validation
    /// before proceeding with further operations. The selection is stored in the application's configuration
    /// for subsequent workflow decisions.
    /// </remarks>
    private void ChooseValidateOnly()
    {
        while (true)
        {
            Console.WriteLine();
            Console.WriteLine("Enter your action");
            Console.WriteLine("1 - copy files");
            Console.WriteLine("2 - only validate");
            var consoleInput = Console.ReadLine();
            if (consoleInput == "q")
            {
                Environment.Exit(0);
            }
            else if (consoleInput == "1")
            {
                WritingOptions.OnlyValidate = false;
                return;
            }
            else if (consoleInput == "2")
            {
                WritingOptions.OnlyValidate = true;
                WritingOptions.Validate = true;
                return;
            }
            else
            {
                Console.WriteLine("Invalid entry");
            }
        }
    }

    /// <summary>
    /// Prompts the user to configure drive validation behavior during the file operation process.
    /// Options include enabling or disabling post-copy validation of files, providing flexibility
    /// based on user requirements or constraints. The method waits for valid user input and updates
    /// the validation configuration accordingly.
    /// </summary>
    /// <remarks>
    /// The user can enable validation to ensure that files are correctly copied or disable it for
    /// faster operation if data verification is not critical. Input options include:
    /// 1 - Enable file validation after copying.
    /// 2 - Disable file validation after copying.
    /// The method repeats prompts for input until valid data is provided or terminates if the user
    /// opts to quit the application.
    /// </remarks>
    private void ValidateDrives()
    {
        while (true)
        {
            Console.WriteLine();
            Console.WriteLine("Enter your selection for validating");
            Console.WriteLine("1 - validate files after copying");
            Console.WriteLine("2 - do not validate");
            var consoleInput = Console.ReadLine();
            if (consoleInput == "q")
            {
                Environment.Exit(0);
            }
            else if (consoleInput == "1")
            {
                WritingOptions.Validate = true;
                return;
            }
            else if (consoleInput == "2")
            {
                WritingOptions.Validate = false;
                return;
            }
            else
            {
                Console.WriteLine("Invalid entry");
            }
        }
    }

    /// <summary>
    /// Prompts the user to input a volume label for the SD card being prepared. If provided, the volume label
    /// is validated to ensure it only contains alphanumeric characters or spaces and does not exceed 15 characters.
    /// The validated label is then saved to the corresponding writing options. The user can choose to skip this step
    /// by leaving the input blank or exit the application by entering 'q'.
    /// </summary>
    private void SetVolumeLabel()
    {
        while (true)
        {
            Console.WriteLine();
            Console.WriteLine("Enter a volume label. Leave blank to skip setting a volume label.");
            var consoleInput = Console.ReadLine();
            if (consoleInput == "q")
            {
                Environment.Exit(0);
            }
            else if (consoleInput != null && !consoleInput.All(c => char.IsLetterOrDigit(c) || c == ' ') &&
                     consoleInput.Length > 15)
            {
                Console.WriteLine(
                    "The volume label must contain only letters, numbers, or spaces and must be less than 16 characters");
            }
            else
            {
                if (consoleInput != null) WritingOptions.VolumeLabel = consoleInput;
                break;
            }
        }
    }

    /// <summary>
    /// Prompts the user to select input folder paths for copying files. Enables entering multiple
    /// folder paths, validates their existence, and adds them to the input options. Allows exiting
    /// the process or finalizing the selection after at least one folder is added.
    /// </summary>
    /// <remarks>
    /// The method continuously prompts the user to enter folder paths or specific commands, such as "q"
    /// to quit or "2" to finish selection. Valid folder paths are added to the internal storage. If a
    /// folder does not exist, an error message is provided, and the user is prompted again.
    /// </remarks>
    private void SelectFolders()
    {
        var folders = new List<string>();


        while (true)
        {
            Console.WriteLine();
            Console.WriteLine("Type the folder path or '2' for finished");
            var consoleInput = Console.ReadLine();
            if (consoleInput == "q")
            {
                Environment.Exit(0);
            }
            else if (consoleInput == "2")
            {
                if (folders.Any())
                {
                    WritingOptions.InputFolder = folders;
                    return;
                }

                Console.WriteLine("You must add a folder or quit the application");
            }
            else
            {
                if (Directory.Exists(consoleInput))
                {
                    folders.Add(consoleInput);
                    Console.WriteLine("Added folder");
                }

                else
                {
                    Console.WriteLine("Folder does not exist");
                }
            }
        }
    }

    /// <summary>
    /// Retrieves and displays a list of available drives on the system, allowing the user to select which drives
    /// they would like to write to. Handles input validation and supports refreshing the drive list, selecting all drives,
    /// or exiting the application. Populates the selected drives into the writing options for subsequent operations.
    /// </summary>
    /// <remarks>
    /// This method interacts with the user via console input to let them choose drives to write data to. It dynamically
    /// refreshes the drive list based on platform-specific drive information and ensures that user selections are properly validated.
    /// Designed to function in a continuous loop until valid input is provided or an exit command is issued.
    /// </remarks>
    private void GetDrivesToWriteTo()
    {
        while (true)
        {
            Console.WriteLine();
            var listViewDrives = new List<DriveInformation>();

            try
            {
                var drives = DriveInfo.GetDrives();
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    foreach (var drive in drives)
                        if (drive is { DriveType: DriveType.Removable, IsReady: true })
                        {
                            var size = (drive.TotalSize / (double)1000000000).ToString("F2");
                            var lvi = new DriveInformation(drive.Name, size);
                            listViewDrives.Add(lvi);
                        }
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    foreach (var drive in drives)
                        if (drive is { DriveType: DriveType.Fixed, IsReady: true, DriveFormat: "msdos" } &&
                            drive.Name.StartsWith("/media"))
                        {
                            var size = (drive.TotalSize / (double)1000000000).ToString("F2");
                            var lvi = new DriveInformation(drive.Name, size);
                            listViewDrives.Add(lvi);
                        }
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Console.WriteLine("OSX is not supported yet for selecting drives.");
                    Environment.Exit(0);
                }
                else
                {
                    Console.WriteLine("Operating system is not identified. Unable to select drive");
                    Environment.Exit(0);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting the drives. Please report this error. {ex.Message}");
            }

            var infoArray = listViewDrives.ToArray();
            Console.WriteLine($"Found the following {infoArray.Length} drives:");
            for (var i = 0; i < infoArray.Length; i++)
                Console.WriteLine($"{i}. {infoArray[i].Name} ({infoArray[i].Size} GB)");

            Console.WriteLine();
            Console.WriteLine("Enter the drive numbers you want to write to, separated by commas. (e.g. 0,1,2)");
            Console.WriteLine("Enter 'a' for all drives");
            Console.WriteLine("Enter 'r' to refresh drives");
            var consoleInput = Console.ReadLine();
            if (consoleInput == "q")
            {
                Environment.Exit(0);
            }
            else if (consoleInput == "a")
            {
                WritingOptions.Drives = listViewDrives.Select(x => x.Name).ToArray();
                return;
            }
            else if (consoleInput == "r")
            {
            }
            else
            {
                if (consoleInput != null)
                {
                    var input = consoleInput.Split(",");
                    if (!input.Any())
                    {
                        Console.WriteLine("You must select a drive to write to");
                    }
                    else
                    {
                        var devices = new List<string>();
                        foreach (var item in input)
                            if (int.TryParse(item, out var number))
                            {
                                if (number < infoArray.Length)
                                    devices.Add(infoArray[number].Name);
                                else
                                    Console.WriteLine($"{item} is an invalid selection");
                            }
                            else
                            {
                                Console.WriteLine($"{item} is not a valid number");
                            }

                        Console.WriteLine();
                        Console.WriteLine($"Selected {devices.Count} drives.");
                        if (!devices.Any()) continue;
                        Console.WriteLine("Enter 1 to try again, q to quit, or anything else to accept");
                        consoleInput = Console.ReadLine();

                        if (consoleInput == "q")
                        {
                            Environment.Exit(0);
                        }
                        else if (consoleInput == "1")
                        {
                        }
                        else
                        {
                            WritingOptions.Drives = devices.ToArray();
                            return;
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Represents information about a drive, specifically its name and size.
    /// </summary>
    /// <remarks>
    /// Instances of this type are used to store and display details about system drives,
    /// such as their identifier (name) and storage capacity (size in GB). The
    /// data is utilized for user selection and drive-related operations in the application.
    /// </remarks>
    private record struct DriveInformation(string Name, string Size);
}