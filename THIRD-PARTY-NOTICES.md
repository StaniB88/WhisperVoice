# Third-Party Notices

Whisper Voice incorporates components from the third-party software projects
listed below. Each component is the property of its respective copyright
holders and is distributed under the license stated next to it.

The full text of common licenses (MIT, Apache 2.0, OFL 1.1) is reproduced at
the end of this file.

---

## Runtime Dependencies (shipped with the application)

| Package | Version | License | Project |
|---------|---------|---------|---------|
| Avalonia | 12.0.3 | MIT | https://github.com/AvaloniaUI/Avalonia |
| Avalonia.Desktop | 12.0.3 | MIT | https://github.com/AvaloniaUI/Avalonia |
| Avalonia.Themes.Fluent | 12.0.3 | MIT | https://github.com/AvaloniaUI/Avalonia |
| Avalonia.Fonts.Inter | 12.0.3 | MIT (package) / OFL 1.1 (Inter font) | https://github.com/AvaloniaUI/Avalonia |
| AvaloniaUI.DiagnosticsSupport | 2.2.1 | MIT | https://github.com/AvaloniaUI/Avalonia |
| CommunityToolkit.Mvvm | 8.4.2 | MIT | https://github.com/CommunityToolkit/dotnet |
| Microsoft.Extensions.DependencyInjection | 10.0.8 | MIT | https://github.com/dotnet/runtime |
| Serilog | 4.3.1 | Apache-2.0 | https://github.com/serilog/serilog |
| Serilog.Sinks.Console | 6.1.1 | Apache-2.0 | https://github.com/serilog/serilog-sinks-console |
| Serilog.Sinks.File | 7.0.0 | Apache-2.0 | https://github.com/serilog/serilog-sinks-file |
| Whisper.net | 1.9.0 | MIT | https://github.com/sandrohanea/whisper.net |
| Whisper.net.Runtime | 1.9.0 | MIT | https://github.com/sandrohanea/whisper.net |
| Whisper.net.Runtime.Cuda | 1.9.0 | MIT | https://github.com/sandrohanea/whisper.net |
| NAudio | 2.3.0 | MIT | https://github.com/naudio/NAudio |
| Velopack | 0.0.1298 | MIT | https://github.com/velopack/velopack |

### Transitively bundled native components

| Component | License | Project |
|-----------|---------|---------|
| whisper.cpp (via Whisper.net.Runtime) | MIT | https://github.com/ggerganov/whisper.cpp |
| ggml (via whisper.cpp) | MIT | https://github.com/ggerganov/ggml |
| SkiaSharp / HarfBuzzSharp (via Avalonia) | MIT | https://github.com/mono/SkiaSharp |
| Skia (native, via SkiaSharp) | BSD-3-Clause | https://skia.org |
| HarfBuzz (native, via HarfBuzzSharp) | MIT | https://harfbuzz.github.io |
| Tmds.DBus.Protocol (via Avalonia, Linux only) | MIT | https://github.com/tmds/Tmds.DBus |
| MicroCom.Runtime (via Avalonia) | MIT | https://github.com/kekekeks/MicroCom |
| Inter font family | SIL Open Font License 1.1 | https://rsms.me/inter/ |

### Models downloaded at runtime

Whisper GGML models are downloaded on demand from Hugging Face and stored
locally under `%APPDATA%/whisper-voice/models/`. They are not bundled in the
installer.

| Component | License | Project |
|-----------|---------|---------|
| OpenAI Whisper model weights | MIT | https://github.com/openai/whisper |

---

## Build / Test Dependencies (not redistributed)

These packages are used only during build and testing. They are not part of
the distributed binary.

| Package | Version | License |
|---------|---------|---------|
| coverlet.collector | 10.0.1 | MIT |
| FluentAssertions | 8.10.0 | Custom (Xceed) — see https://xceed.com/fluent-assertions-faq/ |
| Microsoft.NET.Test.Sdk | 18.5.1 | MIT |
| Moq | 4.20.72 | BSD-3-Clause |
| xunit | 2.9.3 | Apache-2.0 |
| xunit.runner.visualstudio | 3.1.5 | Apache-2.0 |

> **Note:** FluentAssertions 8.x is licensed under a custom Xceed license and
> may require a paid license for commercial use. Only invoked by the test
> project and is not redistributed.

---

## License Texts

### MIT License

```
Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

### Apache License 2.0

The full text is available at https://www.apache.org/licenses/LICENSE-2.0

Summary clauses (excerpt):
```
Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
```

### BSD 3-Clause License

```
Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice,
   this list of conditions and the following disclaimer.

2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

3. Neither the name of the copyright holder nor the names of its contributors
   may be used to endorse or promote products derived from this software
   without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE
LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
POSSIBILITY OF SUCH DAMAGE.
```

### SIL Open Font License 1.1

The full text is available at https://scripts.sil.org/OFL

Summary:
```
This Font Software is licensed under the SIL Open Font License, Version 1.1.
This license is copied below, and is also available with a FAQ at:
https://openfontlicense.org

The OFL allows the licensed fonts to be used, studied, modified and
redistributed freely as long as they are not sold by themselves. The fonts,
including any derivative works, can be bundled, embedded, redistributed
and/or sold with any software provided that any reserved names are not
used by derivative works. The fonts and derivatives, however, cannot be
released under any other type of license. The requirement for fonts to
remain under this license does not apply to any document created using the
fonts or their derivatives.
```

---

For any questions about third-party components, please consult the upstream
project pages linked above. Each upstream project ships its own complete
license text within its source repository and/or NuGet package.
