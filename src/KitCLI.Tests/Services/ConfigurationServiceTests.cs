using FluentAssertions;
using KitCLI.Models;
using KitCLI.Services;
using System.Text.Json;
using System.IO;

namespace KitCLI.Tests.Services;

public class ConfigurationServiceTests : IDisposable
{
    private readonly string _testConfigPath;
    private readonly ConfigurationService _service;
    private readonly TextWriter _originalConsoleOut;

    public ConfigurationServiceTests()
    {
        _testConfigPath = Path.Combine(Path.GetTempPath(), $"kitcli_test_{Guid.NewGuid()}", "config.json");
        _service = new ConfigurationService(_testConfigPath);

        // Capture and suppress console output to prevent interference with other tests
        _originalConsoleOut = Console.Out;
        Console.SetOut(TextWriter.Null);
    }

    public void Dispose()
    {
        // Restore original console output
        Console.SetOut(_originalConsoleOut);

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

    [Fact]
    public async Task SaveConfigAsync_First_Profile_Should_Become_Default()
    {
        // Arrange
        var config = new KitConfig { ApiKey = "first-key" };

        // Act
        await _service.SaveConfigAsync(config, "personal");

        // Assert
        var configFile = await _service.LoadConfigFileAsync();
        configFile.CurrentProfile.Should().Be("personal");
        configFile.Profiles.Should().ContainKey("personal");
    }

    [Fact]
    public async Task LoadConfigAsync_Should_Use_CurrentProfile_When_No_Profile_Specified()
    {
        // Arrange
        var personalConfig = new KitConfig { ApiKey = "personal-key" };
        var workConfig = new KitConfig { ApiKey = "work-key" };

        await _service.SaveConfigAsync(personalConfig, "personal");
        await _service.SaveConfigAsync(workConfig, "work");

        // Set work as current profile
        var configFile = await _service.LoadConfigFileAsync();
        configFile.CurrentProfile = "work";
        await _service.SaveConfigFileAsync(configFile);

        // Act
        var loaded = await _service.LoadConfigAsync();

        // Assert
        loaded.Should().NotBeNull();
        loaded!.ApiKey.Should().Be("work-key");
    }

    [Fact]
    public async Task LoadConfigAsync_Should_Load_Specific_Profile()
    {
        // Arrange
        var personalConfig = new KitConfig { ApiKey = "personal-key" };
        var workConfig = new KitConfig { ApiKey = "work-key" };

        await _service.SaveConfigAsync(personalConfig, "personal");
        await _service.SaveConfigAsync(workConfig, "work");

        // Act
        var loadedPersonal = await _service.LoadConfigAsync("personal");
        var loadedWork = await _service.LoadConfigAsync("work");

        // Assert
        loadedPersonal!.ApiKey.Should().Be("personal-key");
        loadedWork!.ApiKey.Should().Be("work-key");
    }

    [Fact]
    public async Task LoadConfigAsync_Should_Return_Null_For_NonExistent_Profile()
    {
        // Arrange
        var config = new KitConfig { ApiKey = "test-key" };
        await _service.SaveConfigAsync(config, "default");

        // Act
        var loaded = await _service.LoadConfigAsync("nonexistent");

        // Assert
        loaded.Should().BeNull();
    }

    [Fact]
    public async Task SaveConfigAsync_Should_Preserve_CurrentProfile()
    {
        // Arrange
        var config1 = new KitConfig { ApiKey = "key1" };
        var config2 = new KitConfig { ApiKey = "key2" };

        await _service.SaveConfigAsync(config1, "profile1");
        var configFile = await _service.LoadConfigFileAsync();
        var originalProfile = configFile.CurrentProfile;

        // Act
        await _service.SaveConfigAsync(config2, "profile2");

        // Assert
        configFile = await _service.LoadConfigFileAsync();
        configFile.CurrentProfile.Should().Be(originalProfile);
        configFile.Profiles.Should().HaveCount(2);
    }

    [Fact]
    public async Task LoadConfigFileAsync_Should_Return_Empty_ConfigFile_When_No_File()
    {
        // Act
        var configFile = await _service.LoadConfigFileAsync();

        // Assert
        configFile.Should().NotBeNull();
        configFile.Profiles.Should().BeEmpty();
        configFile.CurrentProfile.Should().Be("default");
    }

    [Fact]
    public async Task Multiple_Profiles_Should_Be_Independently_Configurable()
    {
        // Arrange
        var configs = new Dictionary<string, KitConfig>
        {
            ["dev"] = new KitConfig { ApiKey = "dev-key", ApiVersion = "v3" },
            ["staging"] = new KitConfig { ApiKey = "staging-key", ApiVersion = "v4" },
            ["prod"] = new KitConfig { ApiKey = "prod-key", ApiVersion = "v4" }
        };

        // Act
        foreach (var kvp in configs)
        {
            await _service.SaveConfigAsync(kvp.Value, kvp.Key);
        }

        // Assert
        var configFile = await _service.LoadConfigFileAsync();
        configFile.Profiles.Should().HaveCount(3);

        foreach (var kvp in configs)
        {
            var loaded = await _service.LoadConfigAsync(kvp.Key);
            loaded!.ApiKey.Should().Be(kvp.Value.ApiKey);
            loaded.ApiVersion.Should().Be(kvp.Value.ApiVersion);
        }
    }

    [Fact]
    public async Task Environment_Variable_Should_Override_Profile_Config()
    {
        // Arrange
        Environment.SetEnvironmentVariable("KIT_API_KEY", "env-key");
        Environment.SetEnvironmentVariable("KIT_API_VERSION", "v5");

        try
        {
            var profileConfig = new KitConfig { ApiKey = "profile-key" };
            await _service.SaveConfigAsync(profileConfig, "test");

            // Act
            var loaded = await _service.LoadConfigAsync("test");

            // Assert
            loaded.Should().NotBeNull();
            loaded!.ApiKey.Should().Be("env-key");
            loaded.ApiVersion.Should().Be("v5");
        }
        finally
        {
            Environment.SetEnvironmentVariable("KIT_API_KEY", null);
            Environment.SetEnvironmentVariable("KIT_API_VERSION", null);
        }
    }
}
