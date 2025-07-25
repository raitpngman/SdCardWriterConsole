//SPDX-FileCopyrightText: © 2025 Sam Smucker <raitpngman@yahoo.com> 
//SPDX-License-Identifier: BSD-3-Clause

namespace SDCardCreatorConsole;

/// <summary>
/// Provides functionality to set volume labels of storage drives in the windows system.
/// </summary>
public class LabelWin : ILabel
{
    /// <summary>
    /// Sets the volume labels for a specified list of drives.
    /// </summary>
    /// <param name="labelName">The name of the label to set for the drives.</param>
    /// <param name="drives">An array of drive paths to label.</param>
    /// <returns>
    /// True if the operation succeeds for all drives; otherwise, false if any operation fails.
    /// </returns>
    public bool WriteLabel(string labelName, string[] drives)
    {
        var success = true;
        try
        {
            foreach (var drive in drives)
            {
                var driveInfo = new DriveInfo(drive);
                if (driveInfo.IsReady)
                    driveInfo.VolumeLabel = labelName;
                else
                    success = false;
            }
        }
        catch
        {
            success = false;
        }


        return success;
    }
}