//SPDX-FileCopyrightText: © 2025 Sam Smucker <raitpngman@yahoo.com> 
//SPDX-License-Identifier: BSD-3-Clause

namespace SDCardCreatorConsole;

/// <summary>
/// Represents the configuration options for various operations such as validation,
/// file overwriting, cleaning, and other parameters for processing drives and file structures.
/// </summary>
internal class Options
{
    private readonly string _altChar = Path.AltDirectorySeparatorChar.ToString();

    private readonly string _sepChar = Path.DirectorySeparatorChar.ToString();

    /// <summary>
    /// Represents the configuration options for various operations such as validation,
    /// file overwriting, cleaning, and other parameters for processing drives and file structures.
    /// </summary>
    public Options()
    {
        VolumeLabel = "";
        Drives = new string[0];
    }

    /// <summary>
    /// Represents the configuration options for performing operations such as validation,
    /// file overwriting, cleaning storage devices, handling input folders, and other parameters.
    /// </summary>
    public Options(bool validate, Overwrite overWrite, Cleaning cleaningType,
        string[] drives, List<string> inputFolder, string volumeLabel, bool eject)
    {
        Validate = validate;
        OverWriteType = overWrite;
        CleaningType = cleaningType;
        Drives = drives;
        foreach (var item in inputFolder)
        {
            var folder = item;
            if (!string.IsNullOrEmpty(folder))
            {
                if (!folder.EndsWith(_sepChar) && !folder.EndsWith(_altChar)) folder += _sepChar;
                InputFolder.Add(folder);
            }
        }

        VolumeLabel = volumeLabel.Trim();
        Eject = eject;
    }

    /// <summary>
    /// Gets or sets a value indicating whether the validation process should be executed.
    /// When set to true, the application will verify the integrity and correctness
    /// of operations, such as file structure or file content, without performing
    /// further operations unless specified otherwise.
    /// </summary>
    public bool Validate { get; set; }

    /// <summary>
    /// Gets or sets the type of file overwriting behavior during operations.
    /// This property determines how existing files are handled when a conflict occurs:
    /// whether they should never be overwritten, overwritten only if different, or always overwritten.
    /// </summary>
    public Overwrite OverWriteType { get; set; }

    /// <summary>
    /// Gets or sets the cleaning type to be applied to the storage device during operations.
    /// This property determines the cleaning method to be used, such as erasing, formatting,
    /// or performing no cleaning at all.
    /// </summary>
    public Cleaning CleaningType { get; set; }

    /// <summary>
    /// Gets or sets an array of drive identifiers to target for operations such as validation,
    /// file copy, or other related processes. This property specifies the collection of drives
    /// to be used during execution.
    /// </summary>
    public string[] Drives { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the drives should be ejected
    /// after completing the writing or validation processes.
    /// When set to true, the application will attempt to unmount or safely
    /// eject all the specified drives to ensure proper disconnection.
    /// </summary>
    public bool Eject { get; set; }

    /// <summary>
    /// Gets or sets the list of input folder paths for the operation.
    /// Each folder in the list represents a source directory that will be processed.
    /// Paths are automatically normalized to ensure they end with the appropriate directory separator character.
    /// </summary>
    public List<string> InputFolder { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether the process should only perform validation operations.
    /// When set to true, the application will exclusively verify the correctness of files, drives, or structures
    /// without making any changes or proceeding with write operations.
    /// </summary>
    public bool OnlyValidate { get; set; }

    /// <summary>
    /// Gets or sets the volume label of the drive being processed.
    /// The volume label is a textual identifier assigned to a storage medium
    /// to provide a meaningful name that helps in identifying the drive.
    /// </summary>
    public string VolumeLabel { get; set; }
}