using WhisperVoice.Models;
using FluentAssertions;

namespace WhisperVoice.Tests.Models;

public class AppConfigTests
{
    [Fact]
    public void Default_config_has_expected_values()
    {
        var config = new AppConfig();

        config.WhisperModel.Should().Be("base");
        config.Language.Should().Be("de");
        config.AppLanguage.Should().Be("de");
        config.Hotkey.Win.Should().BeTrue();
        config.Hotkey.Key.Should().Be("D");
        config.ToggleMode.Should().BeFalse();
        config.AutoPaste.Should().BeTrue();
        config.ShowFloatingBar.Should().BeTrue();
        config.HasDonated.Should().BeFalse();
        config.SetupComplete.Should().BeFalse();
    }

    [Fact]
    public void HotkeyConfig_default_is_WinD()
    {
        var hotkey = new HotkeyConfig();

        hotkey.Win.Should().BeTrue();
        hotkey.Ctrl.Should().BeFalse();
        hotkey.Shift.Should().BeFalse();
        hotkey.Alt.Should().BeFalse();
        hotkey.Key.Should().Be("D");
        hotkey.VkCode.Should().Be(0x44);
    }
}
