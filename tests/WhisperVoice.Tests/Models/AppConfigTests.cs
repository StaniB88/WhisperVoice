// Copyright (C) 2026 AnyAutomation
//
// This file is part of WhisperVoice.
//
// WhisperVoice is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// WhisperVoice is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public License
// along with WhisperVoice.  If not, see <https://www.gnu.org/licenses/>.

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
