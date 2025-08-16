using System.Diagnostics;
using System.Runtime.InteropServices;
using FluentAssertions;
using Xunit;

namespace KitCLI.Tests.Integration;

// Note: These integration tests require the built executable to run
// They are skipped by default and can be run manually after building
[Trait("Category", "Integration")]
public class HelpCommandIntegrationTests : IDisposable
{
    private readonly string _executablePath;
    private readonly bool _isWindows;

    public HelpCommandIntegrationTests()
    {
        _isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        var configuration = "Debug";
        var runtime = _isWindows ? "win-x64" : "linux-x64";
        var execName = _isWindows ? "kit.exe" : "kit";

        // Try to find the executable
        var projectDir = Directory.GetCurrentDirectory();
        while (!File.Exists(Path.Combine(projectDir, "KitCLI.csproj")) && projectDir != Path.GetPathRoot(projectDir))
        {
            projectDir = Directory.GetParent(projectDir)?.FullName ?? projectDir;
        }

        _executablePath = Path.Combine(projectDir, "..", "KitCLI", "bin", configuration, "net9.0", execName);

        // If debug build doesn't exist, try to build it
        if (!File.Exists(_executablePath))
        {
            // For testing, we'll use dotnet run instead
            _executablePath = "dotnet";
        }
    }

    private (int exitCode, string output) RunCommand(params string[] args)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = _executablePath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        if (_executablePath == "dotnet")
        {
            // Use dotnet run for testing
            var projectPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "KitCLI", "KitCLI.csproj"));
            startInfo.Arguments = $"run --project \"{projectPath}\" -- {string.Join(" ", args)}";
        }
        else
        {
            startInfo.Arguments = string.Join(" ", args);
        }

        using var process = Process.Start(startInfo);
        process?.WaitForExit(5000);

        var output = process?.StandardOutput.ReadToEnd() ?? "";
        var error = process?.StandardError.ReadToEnd() ?? "";
        var fullOutput = output + error;

        return (process?.ExitCode ?? -1, fullOutput);
    }

    [Fact]
    public void MainHelp_ShowsAllCommands()
    {
        // Act
        var (exitCode, output) = RunCommand("--help");

        // Assert
        exitCode.Should().Be(0);
        output.Should().Contain("Usage: kit");
        output.Should().Contain("profile");
        output.Should().Contain("subscriber");
        output.Should().Contain("broadcast");
        output.Should().Contain("tag");
        output.Should().Contain("segment");
    }

    [Fact]
    public void ShortHelpFlag_Works()
    {
        // Act
        var (exitCode, output) = RunCommand("-h");

        // Assert
        exitCode.Should().Be(0);
        output.Should().Contain("Usage: kit");
    }

    [Fact]
    public void CommandHelp_ShowsSubcommands()
    {
        // Act
        var (exitCode, output) = RunCommand("subscriber", "--help");

        // Assert
        exitCode.Should().Be(0);
        output.Should().Contain("Usage: kit subscriber");
        output.Should().Contain("list");
        output.Should().Contain("get");
        output.Should().Contain("search");
        output.Should().Contain("export");
    }

    [Fact]
    public void SubcommandHelp_ShowsOptions()
    {
        // Act
        var (exitCode, output) = RunCommand("subscriber", "list", "--help");

        // Assert
        exitCode.Should().Be(0);
        output.Should().Contain("Usage: kit subscriber list");
        output.Should().Contain("--status");
        output.Should().Contain("--tag");
        output.Should().Contain("--limit");
        output.Should().Contain("--format");
    }

    [Fact]
    public void ProfileAddHelp_ShowsRequiredOptions()
    {
        // Act
        var (exitCode, output) = RunCommand("profile", "add", "--help");

        // Assert
        exitCode.Should().Be(0);
        output.Should().Contain("Required options:");
        output.Should().Contain("--api-key");
    }

    [Fact]
    public void HelpShowsExamples()
    {
        // Act
        var (exitCode, output) = RunCommand("subscriber", "export", "--help");

        // Assert
        exitCode.Should().Be(0);
        output.Should().Contain("Examples:");
        output.Should().Contain("kit subscriber export");
    }

    [Fact]
    public void NoArgs_ShowsHelp()
    {
        // Act
        var (exitCode, output) = RunCommand();

        // Assert
        exitCode.Should().Be(0);
        output.Should().Contain("Usage: kit");
    }

    [Fact]
    public void UnknownCommand_ShowsError()
    {
        // Act
        var (exitCode, output) = RunCommand("unknowncommand");

        // Assert
        exitCode.Should().Be(1);
        output.Should().Contain("Unknown command");
    }

    [Theory]
    [InlineData("config", "--help")]
    [InlineData("profile", "--help")]
    [InlineData("subscriber", "--help")]
    [InlineData("broadcast", "--help")]
    [InlineData("tag", "--help")]
    [InlineData("segment", "--help")]
    [InlineData("sequence", "--help")]
    [InlineData("form", "--help")]
    public void AllMainCommands_RespondToHelp(params string[] args)
    {
        // Act
        var (exitCode, output) = RunCommand(args);

        // Assert
        exitCode.Should().Be(0);
        output.Should().Contain("Usage: kit");
        output.Should().NotContain("No help available");
    }

    public void Dispose()
    {
        // Cleanup if needed
    }
}

