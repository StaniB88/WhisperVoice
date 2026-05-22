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
using WhisperVoice.Services;
using FluentAssertions;

namespace WhisperVoice.Tests.Services;

public class HotkeyServiceTests
{
    [Theory]
    [InlineData(0x41, "A")]
    [InlineData(0x5A, "Z")]
    [InlineData(0x30, "0")]
    [InlineData(0x39, "9")]
    [InlineData(0x70, "F1")]
    [InlineData(0x7B, "F12")]
    [InlineData(0x20, "Space")]
    public void GetKeyName_returns_expected_name(int vkCode, string expected)
    {
        Win32HotkeyService.GetKeyName(vkCode).Should().Be(expected);
    }

    [Theory]
    [InlineData(0x5B, true)]  // LWIN
    [InlineData(0x5C, true)]  // RWIN
    [InlineData(0xA2, true)]  // LCTRL
    [InlineData(0x41, false)] // A
    [InlineData(0x44, false)] // D
    public void IsModifierKey_identifies_modifiers(int vkCode, bool expected)
    {
        Win32HotkeyService.IsModifierKey(vkCode).Should().Be(expected);
    }

    [Fact]
    public void GetVirtualKeyCode_maps_letters_correctly()
    {
        Win32HotkeyService.GetVirtualKeyCode("D").Should().Be(0x44);
        Win32HotkeyService.GetVirtualKeyCode("A").Should().Be(0x41);
        Win32HotkeyService.GetVirtualKeyCode("Space").Should().Be(0x20);
    }
}
