using System.IO;
using FluentAssertions;
using KitCLI.Helpers;
using Xunit;

namespace KitCLI.Tests.Helpers;

public class CommandHelpTests
{
    [Fact]
    public void CheckForHelp_DetectsHelpFlag()
    {
        // Arrange
        var args1 = new[] { "--help" };
        var args2 = new[] { "-h" };
        var args3 = new[] { "list", "--help" };
        var args4 = new[] { "list", "-h", "--format", "json" };
        var args5 = new[] { "list", "--format", "json" };

        // Act & Assert
        CommandHelp.CheckForHelp(args1).Should().BeTrue();
        CommandHelp.CheckForHelp(args2).Should().BeTrue();
        CommandHelp.CheckForHelp(args3).Should().BeTrue();
        CommandHelp.CheckForHelp(args4).Should().BeTrue();
        CommandHelp.CheckForHelp(args5).Should().BeFalse();
    }

    [Fact]
    public void ShowHelp_DisplaysRootHelp()
    {
        // Arrange
        var output = new StringWriter();
        Console.SetOut(output);

        // Act
        CommandHelp.ShowHelp("");

        // Assert
        var result = output.ToString();
        result.Should().Contain("Usage: kit [command] [options]");
        result.Should().Contain("Command-line interface for Kit");
        result.Should().Contain("Subcommands:");
        result.Should().Contain("profile");
        result.Should().Contain("subscriber");
        result.Should().Contain("broadcast");
    }

    [Fact]
    public void ShowHelp_DisplaysCommandHelp()
    {
        // Arrange
        var output = new StringWriter();
        Console.SetOut(output);

        // Act
        CommandHelp.ShowHelp("subscriber");

        // Assert
        var result = output.ToString();
        result.Should().Contain("Usage: kit subscriber <subcommand> [options]");
        result.Should().Contain("Manage and analyze Kit subscribers");
        result.Should().Contain("list");
        result.Should().Contain("get");
        result.Should().Contain("search");
        result.Should().Contain("export");
    }

    [Fact]
    public void ShowHelp_DisplaysSubcommandHelp()
    {
        // Arrange
        var output = new StringWriter();
        Console.SetOut(output);

        // Act
        CommandHelp.ShowHelp("subscriber", "list");

        // Assert
        var result = output.ToString();
        result.Should().Contain("Usage: kit subscriber list [options]");
        result.Should().Contain("List subscribers with optional filtering");
        result.Should().Contain("--status");
        result.Should().Contain("--tag");
        result.Should().Contain("--limit");
        result.Should().Contain("Examples:");
        result.Should().Contain("kit subscriber list --status active");
    }

    [Fact]
    public void ShowHelp_DisplaysRequiredOptions()
    {
        // Arrange
        var output = new StringWriter();
        Console.SetOut(output);

        // Act
        CommandHelp.ShowHelp("profile", "add");

        // Assert
        var result = output.ToString();
        result.Should().Contain("Required options:");
        result.Should().Contain("--api-key, -k <key>");
        result.Should().Contain("Kit API key");
    }

    [Fact]
    public void ShowHelp_DisplaysExamples()
    {
        // Arrange
        var output = new StringWriter();
        Console.SetOut(output);

        // Act
        CommandHelp.ShowHelp("subscriber", "export");

        // Assert
        var result = output.ToString();
        result.Should().Contain("Examples:");
        result.Should().Contain("kit subscriber export --output all-subs.csv");
        result.Should().Contain("kit subscriber export -o active.csv --status active");
    }

    [Fact]
    public void ShowHelp_HandlesUnknownCommand()
    {
        // Arrange
        var output = new StringWriter();
        Console.SetOut(output);

        // Act
        CommandHelp.ShowHelp("unknown", "command");

        // Assert
        var result = output.ToString();
        result.Should().Contain("No help available for 'unknown command'");
    }

    [Fact]
    public void ShowHelpAndReturn_ReturnsZero()
    {
        // Arrange
        var output = new StringWriter();
        Console.SetOut(output);

        // Act
        var result = CommandHelp.ShowHelpAndReturn("subscriber");

        // Assert
        result.Should().Be(0);
        output.ToString().Should().Contain("Usage: kit subscriber");
    }

    [Theory]
    [InlineData("config")]
    [InlineData("profile")]
    [InlineData("subscriber")]
    [InlineData("broadcast")]
    [InlineData("tag")]
    [InlineData("segment")]
    [InlineData("sequence")]
    [InlineData("form")]
    [InlineData("export")]
    public void AllMainCommands_HaveHelpEntries(string command)
    {
        // Arrange
        var output = new StringWriter();
        Console.SetOut(output);

        // Act
        CommandHelp.ShowHelp(command);

        // Assert
        var result = output.ToString();
        result.Should().NotContain("No help available");
        result.Should().Contain($"Usage: kit {command}");
    }

    [Theory]
    [InlineData("subscriber", "list")]
    [InlineData("subscriber", "get")]
    [InlineData("subscriber", "search")]
    [InlineData("subscriber", "export")]
    [InlineData("broadcast", "list")]
    [InlineData("broadcast", "get")]
    [InlineData("broadcast", "stats")]
    [InlineData("profile", "add")]
    [InlineData("profile", "remove")]
    [InlineData("profile", "list")]
    public void AllSubcommands_HaveHelpEntries(string command, string subcommand)
    {
        // Arrange
        var output = new StringWriter();
        Console.SetOut(output);

        // Act
        CommandHelp.ShowHelp(command, subcommand);

        // Assert
        var result = output.ToString();
        result.Should().NotContain("No help available");
        result.Should().Contain($"Usage: kit {command} {subcommand}");
    }

    [Fact]
    public void HelpOutput_FollowsConsistentFormat()
    {
        // Arrange
        var output = new StringWriter();
        Console.SetOut(output);

        // Act
        CommandHelp.ShowHelp("subscriber", "list");

        // Assert
        var result = output.ToString();
        var lines = result.Split('\n');

        // Check format structure
        lines[0].Should().StartWith("Usage:");
        result.Should().Contain("Options:");
        
        // Check indentation
        var optionLines = lines.Where(l => l.StartsWith("  --")).ToList();
        optionLines.Should().NotBeEmpty();
        optionLines.All(l => l.Contains("  ")).Should().BeTrue("all options should be indented");
    }
}