//SPDX-FileCopyrightText: © 2025 Sam Smucker <raitpngman@yahoo.com> 
//SPDX-License-Identifier: BSD-3-Clause

namespace SDCardCreatorConsole;

/// <summary>
/// Represents an interface for setting the volume labels on specified storage drives.
/// </summary>
public interface ILabel
{
    /// <summary>
    /// Sets the volume labels for the specified storage drives.
    /// </summary>
    /// <param name="labelName">The label name to be applied to the drives.</param>
    /// <param name="drives">An array of drive identifiers where the label will be set.</param>
    /// <returns>
    /// A boolean value indicating whether the operation was successful for all specified drives.
    /// Returns true if the operation succeeds for all drives, otherwise false.
    /// </returns>
    public bool WriteLabel(string labelName, string[] drives);
}