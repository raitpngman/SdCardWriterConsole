//SPDX-FileCopyrightText: © 2025 Sam Smucker <raitpngman@yahoo.com> 
//SPDX-License-Identifier: BSD-3-Clause

namespace SDCardCreatorConsole;

/// <summary>
/// Represents a console-based progress bar to display progress updates.
/// Implements the <see cref="System.IProgress{T}"/> interface for reporting progress
/// and <see cref="System.IDisposable"/> interface for resource cleanup.
/// </summary>
public class ProgressBar : IProgress<double>, IDisposable
{
    private const int ProgressBarWidth = 50;
    private const string ProgressCharacter = "█";
    private const string EmptyCharacter = "░";

    private bool _disposed;
    private int _currentProgress;
    private readonly Timer _timer;
    private string _currentText = string.Empty;

    public ProgressBar()
    {
        if (!Console.IsOutputRedirected)
        {
            _timer = new Timer(UpdateProgress, null, TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(100));
        }
    }

    public void Report(double value)
    {
        var progress = (int)(value * 100);
        Interlocked.Exchange(ref _currentProgress, Math.Max(0, Math.Min(100, progress)));
    }

    private void UpdateProgress(object? state)
    {
        var progress = _currentProgress;
        var progressBar = CreateProgressBar(progress);
        var text = $"\r{progressBar} {progress}%";

        if (text != _currentText)
        {
            Console.Write(text);
            _currentText = text;
        }
    }

    private static string CreateProgressBar(int progress)
    {
        var filled = (int)((double)progress / 100 * ProgressBarWidth);
        var empty = ProgressBarWidth - filled;

        return $"[{new string(ProgressCharacter[0], filled)}{new string(EmptyCharacter[0], empty)}]";
    }

    public void Dispose()
    {
        if (_disposed) return;

        lock (this)
        {
            if (!_disposed)
            {
                _timer?.Dispose();
                Console.WriteLine();
                _disposed = true;
            }
        }

        GC.SuppressFinalize(this);
    }
}