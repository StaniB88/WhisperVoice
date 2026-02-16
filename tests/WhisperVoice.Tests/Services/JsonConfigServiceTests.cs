using System.Linq;
using System.Threading.Tasks;
using WhisperVoice.Models;
using WhisperVoice.Services;
using FluentAssertions;

namespace WhisperVoice.Tests.Services;

public class JsonConfigServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly JsonConfigService _service;

    public JsonConfigServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        var configPath = Path.Combine(_tempDir, "config.json");
        _service = new JsonConfigService(configPath);
    }

    [Fact]
    public void Load_returns_default_config_when_no_file_exists()
    {
        var config = _service.Load();
        config.WhisperModel.Should().Be("base");
        config.Hotkey.Win.Should().BeTrue();
    }

    [Fact]
    public void Save_then_load_roundtrips_config()
    {
        var config = new AppConfig
        {
            WhisperModel = "large-v3",
            Language = "en",
            SetupComplete = true
        };

        _service.Save(config);
        var loaded = _service.Load();

        loaded.WhisperModel.Should().Be("large-v3");
        loaded.Language.Should().Be("en");
        loaded.SetupComplete.Should().BeTrue();
    }

    [Fact]
    public void Update_creates_new_config_with_changes()
    {
        var original = _service.Load();
        var updated = _service.Update(c => c with { WhisperModel = "small" });

        updated.WhisperModel.Should().Be("small");
        original.WhisperModel.Should().Be("base"); // immutable
    }

    [Fact]
    public void Load_sanitizes_invalid_model_name()
    {
        var tampered = new AppConfig { WhisperModel = "../../etc/hosts" };
        _service.Save(tampered);

        var loaded = _service.Load();

        loaded.WhisperModel.Should().Be(AppConstants.DefaultWhisperModel);
    }

    [Fact]
    public void Load_sanitizes_out_of_range_vkcode()
    {
        var tampered = new AppConfig { Hotkey = new HotkeyConfig { VkCode = 99999 } };
        _service.Save(tampered);

        var loaded = _service.Load();

        loaded.Hotkey.VkCode.Should().Be(AppConstants.DefaultHotkeyVkCode);
    }

    [Fact]
    public void Load_preserves_valid_config_values()
    {
        var valid = new AppConfig
        {
            WhisperModel = "small",
            Hotkey = new HotkeyConfig { VkCode = 0x41 }
        };
        _service.Save(valid);

        var loaded = _service.Load();

        loaded.WhisperModel.Should().Be("small");
        loaded.Hotkey.VkCode.Should().Be(0x41);
    }

    [Fact]
    public void Load_sanitizes_invalid_language()
    {
        var tampered = new AppConfig { Language = "not-a-language" };
        _service.Save(tampered);

        var loaded = _service.Load();

        loaded.Language.Should().Be(AppConstants.DefaultLanguage);
    }

    [Fact]
    public void Load_preserves_valid_language()
    {
        var valid = new AppConfig { Language = "en" };
        _service.Save(valid);

        var loaded = _service.Load();

        loaded.Language.Should().Be("en");
    }

    [Fact]
    public async Task Update_is_thread_safe_no_lost_updates()
    {
        const int iterations = 100;
        var tasks = Enumerable.Range(0, iterations).Select(_ =>
            Task.Run(() =>
            {
                _service.Update(c => c with
                {
                    Stats = c.Stats with { TotalRecordings = c.Stats.TotalRecordings + 1 }
                });
            }));

        await Task.WhenAll(tasks);

        _service.Current.Stats.TotalRecordings.Should().Be(iterations);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }
}
