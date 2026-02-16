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
}
