using System.Diagnostics;
using FluentAssertions;

namespace KitCLI.Tests.Integration;

public class SequenceHelpRoutingTests
{
    [Theory]
    [MemberData(nameof(SequenceHelpCommands))]
    public async Task Sequence_Help_Commands_Should_Not_Require_Configuration(
        string[] command,
        string expectedUsage)
    {
        var (exitCode, output) = await RunCommand(command);

        exitCode.Should().Be(0);
        output.Should().Contain(expectedUsage);
        output.Should().NotContain("Invalid or missing configuration");
    }

    public static IEnumerable<object[]> SequenceHelpCommands =>
    [
        [new[] { "sequence", "emails", "--help" }, "Usage: kit sequence emails <id> [options]"],
        [new[] { "sequence", "email", "--help" }, "Usage: kit sequence email <subcommand> [options]"],
        [new[] { "sequence", "email", "get", "--help" }, "Usage: kit sequence email get <sequence-id> <email-id> [options]"],
        [new[] { "sequence", "email", "update", "--help" }, "Usage: kit sequence email update <sequence-id> <email-id> (--subject <text> | --content-file <path>) [options]"]
    ];

    private static async Task<(int ExitCode, string Output)> RunCommand(string[] command)
    {
        var testOutputDirectory = new FileInfo(typeof(SequenceHelpRoutingTests).Assembly.Location).Directory;
        var configuration = testOutputDirectory?.Parent?.Name;
        configuration.Should().NotBeNull("the test output path includes the active build configuration");
        var outputDirectory = Path.Combine(FindSolutionRoot(), "src", "KitCLI", "bin", configuration!, "net10.0");
        var assemblyPath = Directory.EnumerateFiles(outputDirectory, "kit.dll", SearchOption.AllDirectories).SingleOrDefault();
        assemblyPath.Should().NotBeNull($"the test project reference builds kit.dll under {outputDirectory}");

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add(assemblyPath!);
        foreach (var argument in command)
        {
            startInfo.ArgumentList.Add(argument);
        }

        startInfo.Environment["KIT_CONFIG_PATH"] = Path.Combine(Path.GetTempPath(), $"kit-cli-{Guid.NewGuid():N}.json");
        startInfo.Environment.Remove("KIT_API_KEY");
        startInfo.Environment.Remove("KIT_CLI_VERBOSE");

        using var process = Process.Start(startInfo);
        process.Should().NotBeNull();

        var standardOutput = process!.StandardOutput.ReadToEndAsync();
        var standardError = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        return (process.ExitCode, await standardOutput + await standardError);
    }

    private static string FindSolutionRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "KitCLI.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException("Could not find the KitCLI solution root.");
    }
}
