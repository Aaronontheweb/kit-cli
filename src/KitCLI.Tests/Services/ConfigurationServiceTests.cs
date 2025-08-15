using FluentAssertions;
using KitCLI.Models;
using KitCLI.Services;
using System.Text.Json;

namespace KitCLI.Tests.Services;

public class ConfigurationServiceTests : IDisposable
{
    private readonly string _testConfigPath;
    private readonly ConfigurationService _service;

    public ConfigurationServiceTests()
    {
        _testConfigPath = Path.Combine(Path.GetTempPath(), $"kitcli_test_{Guid.NewGuid()}", "config.json");
        _service = new ConfigurationService(_testConfigPath);
    }

    public void Dispose()
    {
        var dir = Path.GetDirectoryName(_testConfigPath);
        if (dir != null && Directory.Exists(dir))
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public async Task SaveConfigAsync_Should_Create_Config_File()
    {
        // Arrange
        var config = new KitConfig
        {
            ApiKey = "test-key-123",
            ApiVersion = "v4"
        };

        // Act
        await _service.SaveConfigAsync(config);

        // Assert
        File.Exists(_testConfigPath).Should().BeTrue();

        var savedContent = await File.ReadAllTextAsync(_testConfigPath);
        var savedConfigFile = JsonSerializer.Deserialize(savedContent, KitJsonContext.Default.ConfigFile);

        savedConfigFile.Should().NotBeNull();
        savedConfigFile!.CurrentProfile.Should().Be("default");
        savedConfigFile.Profiles.Should().ContainKey("default");
        savedConfigFile.Profiles["default"].ApiKey.Should().Be("test-key-123");
    }

    [Fact]
    public async Task SaveConfigAsync_Should_Support_Multiple_Profiles()
    {
        // Arrange
        var defaultConfig = new KitConfig { ApiKey = "default-key" };
        var prodConfig = new KitConfig { ApiKey = "prod-key" };

        // Act
        await _service.SaveConfigAsync(defaultConfig, "default");
        await _service.SaveConfigAsync(prodConfig, "production");

        // Assert
        var loadedDefault = await _service.LoadConfigAsync("default");
        var loadedProd = await _service.LoadConfigAsync("production");

        loadedDefault!.ApiKey.Should().Be("default-key");
        loadedProd!.ApiKey.Should().Be("prod-key");
    }

    [Fact]
    public async Task LoadConfigAsync_Should_Return_Null_When_No_Config()
    {
        // Act
        var config = await _service.LoadConfigAsync();

        // Assert
        config.Should().BeNull();
    }

    [Fact]
    public async Task LoadConfigAsync_Should_Use_Default_Profile()
    {
        // Arrange
        var config = new KitConfig { ApiKey = "test-key" };
        await _service.SaveConfigAsync(config);

        // Act
        var loaded = await _service.LoadConfigAsync();

        // Assert
        loaded.Should().NotBeNull();
        loaded!.ApiKey.Should().Be("test-key");
    }

    [Fact]
    public void GetConfigPath_Should_Return_Correct_Path()
    {
        // Act
        var path = _service.GetConfigPath();

        // Assert
        path.Should().Be(_testConfigPath);
    }

    [Fact]
    public void KitConfig_IsValid_Should_Return_True_When_ApiKey_Present()
    {
        // Arrange
        var config = new KitConfig { ApiKey = "some-key" };

        // Act & Assert
        config.IsValid.Should().BeTrue();
    }

    [Fact]
    public void KitConfig_IsValid_Should_Return_False_When_ApiKey_Empty()
    {
        // Arrange
        var config = new KitConfig { ApiKey = "" };

        // Act & Assert
        config.IsValid.Should().BeFalse();
    }

    [Fact]
    public void KitConfig_GetMaskedApiKey_Should_Mask_Key_Properly()
    {
        // Arrange
        var config = new KitConfig { ApiKey = "secret-api-key-12345" };

        // Act
        var masked = config.GetMaskedApiKey();

        // Assert
        masked.Should().Be("secr...2345");
    }

    [Fact]
    public void KitConfig_GetMaskedApiKey_Should_Handle_Short_Keys()
    {
        // Arrange
        var config = new KitConfig { ApiKey = "key" };

        // Act
        var masked = config.GetMaskedApiKey();

        // Assert
        masked.Should().Be("****");
    }
}
