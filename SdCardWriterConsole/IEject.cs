//SPDX-FileCopyrightText: © 2025 Sam Smucker <raitpngman@yahoo.com> 
//SPDX-License-Identifier: BSD-3-Clause

namespace SDCardCreatorConsole;

/// <summary>
/// Represents an interface for handling the ejection of drives.
/// </summary>
public interface IEject
{
    /// <summary>
    /// Attempts to unmount the specified drives.
    /// </summary>
    /// <param name="driveLetters">An array of drive letters representing the drives to be unmounted.</param>
    /// <returns>True if the drives are successfully unmounted, otherwise false.</returns>
    bool Unmount(string[] driveLetters);
}