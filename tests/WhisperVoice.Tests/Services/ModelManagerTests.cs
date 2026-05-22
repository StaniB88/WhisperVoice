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

namespace WhisperVoice.Tests.Services;

public class ModelManagerTests : IDisposable
{
    private readonly string _tempDir;
    private readonly ModelManager _manager;

    public ModelManagerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        _manager = new ModelManager(_tempDir);
    }

    [Fact]
    public void GetModelPath_returns_path_inside_models_directory()
    {
        var path = _manager.GetModelPath("base");

        path.Should().StartWith(Path.GetFullPath(_tempDir));
        Path.GetFileName(path).Should().Be("ggml-base.bin");
    }

    [Theory]
    [InlineData("../../etc/hosts")]
    [InlineData("..\\..\\Windows\\System32\\cmd")]
    public void GetModelPath_rejects_path_traversal(string maliciousName)
    {
        var act = () => _manager.GetModelPath(maliciousName);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Invalid model name*");
    }

    [Fact]
    public void GetModelPath_rejects_name_with_directory_separators()
    {
        var act = () => _manager.GetModelPath("sub/base");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Invalid model name*");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }
}
