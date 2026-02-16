using System;
using WhisperVoice.Models;

namespace WhisperVoice.Services;

public interface IConfigService
{
    AppConfig Load();
    void Save(AppConfig config);
    AppConfig Update(Func<AppConfig, AppConfig> updater);
    AppConfig Current { get; }
}
