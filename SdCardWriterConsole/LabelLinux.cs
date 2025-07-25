using System.Diagnostics;
//SPDX-FileCopyrightText: © 2025 Sam Smucker <raitpngman@yahoo.com> 
//SPDX-License-Identifier: BSD-3-Clause


namespace SDCardCreatorConsole;

/// <summary>
/// A class that implements the <see cref="ILabel"/> interface to set volume labels on storage drives in Linux systems.
/// </summary>
public class LabelLinux : ILabel
{
    /// <summary>
    /// Writes a volume label to the specified storage drives.
    /// </summary>
    /// <param name="labelName">The volume label to be written to the drives.</param>
    /// <param name="drives">An array of drive paths where the volume label is to be set.</param>
    /// <returns>
    /// A boolean value indicating whether the operation was successful.
    /// Returns <c>true</c> if the volume label was successfully written to all drives; otherwise, <c>false</c>.
    /// </returns>
    public bool WriteLabel(string labelName, string[] drives)
    {
        var success = true;
        var devDrives = GetDevDrives(drives);
        if (devDrives.Count != drives.Length) success = false;
        var command = "-c \" ";
        foreach (var drive in devDrives)
        {
            var cmd = $"sudo mlabel -i {drive} ::{labelName};";
            command += cmd;
        }

        command += " \"";
        var psi = new ProcessStartInfo("/bin/bash", command);
        psi.RedirectStandardOutput = true;
        psi.UseShellExecute = false;
        //psi.CreateNoWindow = false;
        var p = Process.Start(psi);
        //var output = p.StandardOutput.ReadToEnd();
        if (p != null)
        {
            p.WaitForExit();
            if (p.ExitCode != 0) success = false;
        }


        return success;
    }

    /// <summary>
    /// Retrieves the physical device paths corresponding to a given set of mounted drives.
    /// </summary>
    /// <param name="drives">An array of paths to the mounted drives for which the device paths are to be obtained.</param>
    /// <returns>
    /// A list of strings containing the device paths associated with the given mounted drives.
    /// The list may be empty if no valid device paths are found.
    /// </returns>
    private List<string> GetDevDrives(string[] drives)
    {
        var devDrives = new List<string>(drives.Length);
        //Get the /dev drives
        for (var i = 0; i < drives.Length; i++)
        {
            var command = $"-c \"findmnt -n -o SOURCE {drives[i]}\"";
            //string command = $"\"findmnt -n -o SOURCE {drives[i]}\"";
            var psi = new ProcessStartInfo("/bin/bash", command);
            psi.RedirectStandardOutput = true;
            psi.UseShellExecute = false;
            var p = Process.Start(psi);
            if (p != null)
            {
                var output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
                if (p.ExitCode == 0) devDrives.Add(output.Trim());
            }
        }

        return devDrives;
    }
}