//SPDX-FileCopyrightText: © 2025 Sam Smucker <raitpngman@yahoo.com> 
//SPDX-License-Identifier: BSD-3-Clause

namespace SDCardCreatorConsole;

/// <summary>
/// Specifies options for handling file overwriting behavior during operations.
/// </summary>
public enum Overwrite
{
    Never,
    Different,
    Always
}

/// <summary>
/// Defines the cleaning options to be applied to the storage device during operations.
/// </summary>
public enum Cleaning
{
    None,
    Erase,
    Format
}