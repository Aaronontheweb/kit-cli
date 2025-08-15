using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;

namespace KitCLI.Services;

/// <summary>
/// Handles self-updating the CLI binary
/// </summary>
public sealed class SelfUpdater
{
    public async Task<bool> PerformUpdateAsync(UpdateInfo update, byte[] downloadedData)
    {
        var currentExePath = Environment.ProcessPath
            ?? throw new InvalidOperationException("Cannot determine current executable path");

        var tempDir = Path.Combine(Path.GetTempPath(), $"kit-update-{update.Version}");
        var extractedPath = Path.Combine(tempDir, "kit");
        var backupPath = currentExePath + ".backup";

        try
        {
            // Create temp directory
            Directory.CreateDirectory(tempDir);

            // Extract the downloaded file
            var extractSuccess = await ExtractUpdateAsync(update.AssetName, downloadedData, tempDir, extractedPath);
            if (!extractSuccess)
            {
                Console.Error.WriteLine("❌ Failed to extract update package");
                return false;
            }

            // Make sure the extracted file exists
            if (!File.Exists(extractedPath))
            {
                // On Windows, the executable might have .exe extension
                if (OperatingSystem.IsWindows())
                {
                    extractedPath = Path.Combine(tempDir, "kit.exe");
                    if (!File.Exists(extractedPath))
                    {
                        Console.Error.WriteLine($"❌ Extracted file not found at {extractedPath}");
                        return false;
                    }
                }
                else
                {
                    Console.Error.WriteLine($"❌ Extracted file not found at {extractedPath}");
                    return false;
                }
            }

            // Platform-specific replacement
            if (OperatingSystem.IsWindows())
            {
                return await UpdateOnWindows(currentExePath, extractedPath, backupPath);
            }
            else
            {
                return await UpdateOnUnix(currentExePath, extractedPath, backupPath);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"❌ Update failed: {ex.Message}");

            // Restore backup if it exists
            if (File.Exists(backupPath))
            {
                try
                {
                    File.Move(backupPath, currentExePath, true);
                    Console.WriteLine("✓ Restored previous version from backup");
                }
                catch
                {
                    Console.Error.WriteLine("⚠️  Could not restore backup");
                }
            }

            return false;
        }
        finally
        {
            // Cleanup
            try
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
            catch { }
        }
    }

    private async Task<bool> ExtractUpdateAsync(string assetName, byte[] data, string tempDir, string outputPath)
    {
        try
        {
            var tempFile = Path.Combine(tempDir, assetName);
            await File.WriteAllBytesAsync(tempFile, data);

            if (assetName.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase))
            {
                // Extract tar.gz (Unix)
                return await ExtractTarGzAsync(tempFile, tempDir);
            }
            else if (assetName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                // Extract zip (Windows)
                ZipFile.ExtractToDirectory(tempFile, tempDir, true);
                return true;
            }
            else
            {
                // Assume it's the raw binary
                File.Move(tempFile, outputPath, true);
                return true;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Extraction failed: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> ExtractTarGzAsync(string tarGzPath, string outputDir)
    {
        try
        {
            // Use tar command for extraction
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "tar",
                    Arguments = $"-xzf \"{tarGzPath}\" -C \"{outputDir}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            await process.WaitForExitAsync();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> UpdateOnWindows(string currentPath, string newPath, string backupPath)
    {
        // Windows strategy: Use a batch file to replace the executable after this process exits
        var batchFile = Path.Combine(Path.GetTempPath(), "kit-update.bat");
        var batchContent = $@"@echo off
echo Updating Kit CLI...
timeout /t 2 /nobreak > nul
move /y ""{currentPath}"" ""{backupPath}"" > nul 2>&1
move /y ""{newPath}"" ""{currentPath}"" > nul 2>&1
echo Update complete!
""{currentPath}"" --version
del ""{backupPath}"" > nul 2>&1
del ""%~f0""
";
        await File.WriteAllTextAsync(batchFile, batchContent);

        Process.Start(new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c \"\"{batchFile}\"\"",
            UseShellExecute = true,
            CreateNoWindow = false,
            WindowStyle = ProcessWindowStyle.Normal
        });

        Console.WriteLine("✓ Update process started. Kit CLI will restart automatically...");
        Environment.Exit(0);
        return true;
    }

    private async Task<bool> UpdateOnUnix(string currentPath, string newPath, string backupPath)
    {
        // Unix strategy: Use a shell script to replace the executable after this process exits
        var scriptFile = Path.Combine(Path.GetTempPath(), "kit-update.sh");
        var scriptContent = $@"#!/bin/bash
echo 'Updating Kit CLI...'
sleep 2
mv ""{currentPath}"" ""{backupPath}"" 2>/dev/null
mv ""{newPath}"" ""{currentPath}""
chmod +x ""{currentPath}""
echo 'Update complete!'
""{currentPath}"" --version
rm -f ""{backupPath}""
rm -- ""$0""
";
        await File.WriteAllTextAsync(scriptFile, scriptContent);

        // Make script executable
        var chmodProcess = Process.Start("chmod", $"+x \"{scriptFile}\"");
        if (chmodProcess != null)
        {
            await chmodProcess.WaitForExitAsync();
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = "/bin/bash",
            Arguments = $"\"{scriptFile}\"",
            UseShellExecute = false,
            CreateNoWindow = false
        });

        Console.WriteLine("✓ Update process started. Kit CLI will restart automatically...");
        Environment.Exit(0);
        return true;
    }

    /// <summary>
    /// Verify that we have write permissions to update the binary
    /// </summary>
    public static bool CanUpdate()
    {
        var currentExePath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(currentExePath))
        {
            return false;
        }

        var directory = Path.GetDirectoryName(currentExePath);
        if (string.IsNullOrEmpty(directory))
        {
            return false;
        }

        try
        {
            // Try to create a temp file in the same directory
            var tempFile = Path.Combine(directory, $".kit-update-test-{Guid.NewGuid()}");
            File.WriteAllText(tempFile, "test");
            File.Delete(tempFile);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

