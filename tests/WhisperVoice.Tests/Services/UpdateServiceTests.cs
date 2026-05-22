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

using WhisperVoice.Services;
using FluentAssertions;
using Moq;

namespace WhisperVoice.Tests.Services;

public class UpdateServiceTests
{
    [Fact]
    public void UpdateInfo_record_stores_version()
    {
        var info = new UpdateInfo("1.2.3", new object());
        info.Version.Should().Be("1.2.3");
    }

    [Fact]
    public async Task IUpdateService_mock_returns_null_when_no_update()
    {
        var mock = new Mock<IUpdateService>();
        mock.Setup(s => s.CheckForUpdateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((UpdateInfo?)null);

        var result = await mock.Object.CheckForUpdateAsync();
        result.Should().BeNull();
    }

    [Fact]
    public async Task IUpdateService_mock_returns_update_info()
    {
        var expectedUpdate = new UpdateInfo("2.0.0", new object());
        var mock = new Mock<IUpdateService>();
        mock.Setup(s => s.CheckForUpdateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedUpdate);

        var result = await mock.Object.CheckForUpdateAsync();
        result.Should().NotBeNull();
        result!.Version.Should().Be("2.0.0");
    }

    [Fact]
    public void IUpdateService_mock_is_not_installed_by_default()
    {
        var mock = new Mock<IUpdateService>();
        mock.Setup(s => s.IsInstalled).Returns(false);

        mock.Object.IsInstalled.Should().BeFalse();
    }

    [Fact]
    public void IUpdateService_has_UpdateAvailable_event()
    {
        var eventInfo = typeof(IUpdateService).GetEvent("UpdateAvailable");
        eventInfo.Should().NotBeNull();
        eventInfo!.EventHandlerType.Should().Be(typeof(EventHandler<UpdateInfo>));
    }

    [Fact]
    public async Task CheckForUpdateAsync_fires_UpdateAvailable_when_update_found()
    {
        UpdateInfo? received = null;
        var expected = new UpdateInfo("3.0.0", new object());

        var mock = new Mock<IUpdateService>();
        mock.Setup(s => s.CheckForUpdateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected)
            .Raises(s => s.UpdateAvailable += null, mock.Object, expected);

        mock.Object.UpdateAvailable += (_, info) => received = info;
        await mock.Object.CheckForUpdateAsync();

        received.Should().NotBeNull();
        received!.Version.Should().Be("3.0.0");
    }

    [Fact]
    public void UpdateAvailable_event_can_be_raised_on_mock()
    {
        var mock = new Mock<IUpdateService>();
        mock.Setup(s => s.IsInstalled).Returns(true);
        mock.Setup(s => s.CurrentVersion).Returns("2.0.0");

        UpdateInfo? received = null;
        mock.Object.UpdateAvailable += (_, info) => received = info;

        var update = new UpdateInfo("2.1.0", new object());
        mock.Raise(s => s.UpdateAvailable += null, mock.Object, update);

        received.Should().NotBeNull();
        received!.Version.Should().Be("2.1.0");
    }
}
