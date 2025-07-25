//SPDX-FileCopyrightText: © 2025 Sam Smucker <raitpngman@yahoo.com> 
//SPDX-License-Identifier: BSD-3-Clause

using System.Diagnostics;

namespace SDCardCreatorConsole;

/// <summary>
/// Represents a class to handle the ejection of USB drives on Linux systems.
/// </summary>
public class UsbEjectLinux : IEject
{
    /// <summary>
    /// Unmounts the specified drives on a Linux system.
    /// </summary>
    /// <param name="drives">An array of drive paths to unmount.</param>
    /// <returns>True if all drives are unmounted successfully; otherwise, false.</returns>
    public bool Unmount(string[] drives)
    {
        var success = true;
        var command = "-c \" ";
        foreach (var drive in drives)
        {
            var cmd = $"sudo umount -l {drive};";
            command += cmd;
        }

        command += " \"";
        var psi = new ProcessStartInfo("/bin/bash", command);
        psi.RedirectStandardOutput = true;
        psi.UseShellExecute = false;
        //psi.CreateNoWindow = false;
        var p = Process.Start(psi);

        if (p != null)
        {
            p.WaitForExit();
            if (p.ExitCode != 0) success = false;
        }


        //// For lazy unmount
        // string lazyUnmountCommand = "sudo umount -l " + mountPoint;

        // Create a new process start info
        /*ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = "sudo",
            Arguments = $"umount -l {mountPoint}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        // Start the process
        using (Process process = new Process { StartInfo = startInfo })
        {
            process.Start();

            // Read the output and error streams
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            // Wait for the process to exit
            process.WaitForExit();

            // Check the exit code
            if (process.ExitCode == 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }*/

        return success;
    }
}