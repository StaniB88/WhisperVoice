const { app, BrowserWindow, ipcMain, clipboard, Tray, Menu, nativeImage, screen } = require('electron');
const path = require('path');
const fs = require('fs');
const { spawn, execSync } = require('child_process');
const http = require('http');
const https = require('https');
const net = require('net');
const { uIOhook, UiohookKey } = require('uiohook-napi');
const extract = require('extract-zip');

// Set app name explicitly (shows in system tray and task manager)
app.name = 'Whisper Voice';
if (app.setName) app.setName('Whisper Voice');

// Set Windows App User Model ID (important for taskbar/tray identification)
if (process.platform === 'win32') {
  app.setAppUserModelId('com.local.whisper-voice');
}

// ==================== SINGLE INSTANCE LOCK ====================
// Prevent multiple instances of the app from running
const gotTheLock = app.requestSingleInstanceLock();

if (!gotTheLock) {
  // Another instance is already running, quit this one
  console.log('Another instance is already running. Quitting...');
  app.quit();
} else {
  // This is the first instance - handle second-instance event
  app.on('second-instance', (event, commandLine, workingDirectory) => {
    // Someone tried to run a second instance, focus our window instead
    if (mainWindow) {
      if (mainWindow.isMinimized()) mainWindow.restore();
      mainWindow.show();
      mainWindow.focus();
    }
  });
}

let mainWindow;
let setupWindow;
let floatingBar;
let tray;
let whisperServer = null;
let helperProcess = null;
let helperSocket = null;
let helperServer = null;
let isRecording = false;

// Push-to-Talk State
let keysPressed = {
  ctrl: false,
  shift: false,
  alt: false,
  win: false,
  key: false
};
let hotkeyTriggered = false;  // Verhindert wiederholtes Auslösen bei gehaltener Taste

// Pfade
const SERVER_PORT = 5555;
const HELPER_PORT = 5556;
const CONFIG_PATH = path.join(app.getPath('userData'), 'config.json');
const INSTALL_PATH = path.join(app.getPath('userData'), 'dependencies');

// Icon path - use .ico for Windows (absolute path required)
function getIconPath() {
  const icoPath = path.resolve(path.join(__dirname, '..', 'build', 'icon.ico'));
  const pngPath = path.resolve(path.join(__dirname, '..', 'assets', 'logo.png'));
  // Prefer .ico on Windows, fall back to .png
  if (process.platform === 'win32' && fs.existsSync(icoPath)) {
    console.log('Using icon:', icoPath);
    return icoPath;
  }
  console.log('Using icon:', pngPath);
  return pngPath;
}

// Download URLs
const PYTHON_URL = 'https://www.python.org/ftp/python/3.12.0/python-3.12.0-embed-amd64.zip';
const FFMPEG_URL = 'https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-gpl.zip';
const GET_PIP_URL = 'https://bootstrap.pypa.io/get-pip.py';

// Standard-Konfiguration
const DEFAULT_CONFIG = {
  // Setup
  setupComplete: false,
  pythonPath: null,
  ffmpegPath: null,
  cudaEnabled: false,

  // Hotkey
  hotkey: {
    ctrl: false,
    shift: false,
    alt: false,
    win: true,
    key: 'D'
  },
  hotkeyDisplay: 'Win+D (halten)',

  // Whisper
  whisperModel: 'medium',
  language: 'de',

  // App
  appLanguage: 'de',
  autoPaste: true,
  showFloatingBar: true,
  toggleMode: false,

  // Stats
  stats: {
    totalRecordings: 0,
    totalWords: 0
  },
  notes: []
};

// Dynamischer Python-Pfad
function getPythonPath() {
  if (CONFIG.pythonPath && fs.existsSync(CONFIG.pythonPath)) {
    return CONFIG.pythonPath;
  }
  // Fallback: Suche Python in Standard-Installationspfaden
  const possiblePaths = [
    path.join(INSTALL_PATH, 'python', 'python.exe'),
    path.join(process.env.LOCALAPPDATA || '', 'Programs', 'Python', 'Python312', 'python.exe'),
    path.join(process.env.LOCALAPPDATA || '', 'Programs', 'Python', 'Python311', 'python.exe'),
    path.join(process.env.LOCALAPPDATA || '', 'Programs', 'Python', 'Python310', 'python.exe'),
    'python.exe', // Falls in PATH
  ];
  for (const pythonPath of possiblePaths) {
    if (fs.existsSync(pythonPath)) {
      return pythonPath;
    }
  }
  return null;
}

// Find all system Python installations
function findSystemPythonPaths() {
  const possiblePaths = [
    // User install locations (most common)
    path.join(process.env.LOCALAPPDATA || '', 'Programs', 'Python', 'Python313', 'python.exe'),
    path.join(process.env.LOCALAPPDATA || '', 'Programs', 'Python', 'Python312', 'python.exe'),
    path.join(process.env.LOCALAPPDATA || '', 'Programs', 'Python', 'Python311', 'python.exe'),
    path.join(process.env.LOCALAPPDATA || '', 'Programs', 'Python', 'Python310', 'python.exe'),
    // System-wide install locations (Program Files)
    'C:\\Program Files\\Python313\\python.exe',
    'C:\\Program Files\\Python312\\python.exe',
    'C:\\Program Files\\Python311\\python.exe',
    'C:\\Program Files\\Python310\\python.exe',
    'C:\\Program Files (x86)\\Python313\\python.exe',
    'C:\\Program Files (x86)\\Python312\\python.exe',
    'C:\\Program Files (x86)\\Python311\\python.exe',
    'C:\\Program Files (x86)\\Python310\\python.exe',
    // Legacy root install locations
    'C:\\Python313\\python.exe',
    'C:\\Python312\\python.exe',
    'C:\\Python311\\python.exe',
    'C:\\Python310\\python.exe',
  ];
  const found = possiblePaths.filter(p => fs.existsSync(p));

  // Also try to find Python via PATH using 'where' command
  if (found.length === 0) {
    try {
      const { execSync } = require('child_process');
      const whereResult = execSync('where python', { encoding: 'utf8', timeout: 5000 });
      const pathsFromWhere = whereResult.trim().split('\n')
        .map(p => p.trim())
        .filter(p => p.endsWith('python.exe') && fs.existsSync(p))
        // Exclude WindowsApps stub (it's not a real Python)
        .filter(p => !p.includes('WindowsApps'));
      return pathsFromWhere;
    } catch (e) {
      // 'where' command failed, no Python in PATH
    }
  }

  return found;
}

// Check if a Python installation has working PyTorch and Whisper
function checkPythonInstallation(pythonPath) {
  return new Promise((resolve) => {
    const script = `
import json
import sys
result = {
    'python': sys.version,
    'pythonPath': sys.executable,
    'hasTorch': False,
    'hasWhisper': False,
    'torchVersion': None,
    'cudaVersion': None,
    'cudaAvailable': False,
    'gpuName': None,
    'gpuWorks': False
}
try:
    import torch
    result['hasTorch'] = True
    result['torchVersion'] = torch.__version__
    result['cudaVersion'] = torch.version.cuda
    result['cudaAvailable'] = torch.cuda.is_available()
    if torch.cuda.is_available():
        result['gpuName'] = torch.cuda.get_device_name(0)
        try:
            x = torch.randn(10, 10, device='cuda')
            y = torch.mm(x, x)
            torch.cuda.synchronize()
            result['gpuWorks'] = True
        except:
            result['gpuWorks'] = False
except ImportError:
    pass
try:
    import whisper
    result['hasWhisper'] = True
except ImportError:
    pass
print(json.dumps(result))
`;
    const python = spawn(pythonPath, ['-c', script], { timeout: 30000 });
    let output = '';

    python.stdout.on('data', (data) => { output += data.toString(); });
    python.stderr.on('data', () => {}); // Ignore warnings

    python.on('close', () => {
      try {
        const result = JSON.parse(output.trim());
        result.path = pythonPath;
        resolve(result);
      } catch {
        resolve({ path: pythonPath, hasTorch: false, hasWhisper: false });
      }
    });

    python.on('error', () => {
      resolve({ path: pythonPath, hasTorch: false, hasWhisper: false });
    });
  });
}

// Detect existing Python installations with PyTorch/Whisper
async function detectExistingInstallations() {
  const systemPythons = findSystemPythonPaths();
  const results = [];

  for (const pythonPath of systemPythons) {
    const info = await checkPythonInstallation(pythonPath);
    if (info.hasTorch || info.hasWhisper) {
      results.push(info);
    }
  }

  // Sort by best installation (has both, GPU works, newest torch)
  results.sort((a, b) => {
    // Prefer installations where GPU works
    if (a.gpuWorks !== b.gpuWorks) return b.gpuWorks ? 1 : -1;
    // Prefer installations with both torch and whisper
    const aComplete = a.hasTorch && a.hasWhisper;
    const bComplete = b.hasTorch && b.hasWhisper;
    if (aComplete !== bComplete) return bComplete ? 1 : -1;
    return 0;
  });

  return results;
}

// Konfiguration laden
function loadConfig() {
  try {
    if (fs.existsSync(CONFIG_PATH)) {
      const data = fs.readFileSync(CONFIG_PATH, 'utf8');
      return { ...DEFAULT_CONFIG, ...JSON.parse(data) };
    }
  } catch (err) {
    console.error('Config laden fehlgeschlagen:', err);
  }
  return { ...DEFAULT_CONFIG };
}

// Konfiguration speichern
function saveConfig() {
  try {
    fs.writeFileSync(CONFIG_PATH, JSON.stringify(CONFIG, null, 2));
  } catch (err) {
    console.error('Config speichern fehlgeschlagen:', err);
  }
}

let CONFIG = DEFAULT_CONFIG;

// ============================================
// SETUP FUNKTIONEN
// ============================================

function createSetupWindow() {
  setupWindow = new BrowserWindow({
    width: 650,
    height: 850,
    resizable: true,
    frame: false,
    icon: getIconPath(),
    webPreferences: {
      nodeIntegration: true,
      contextIsolation: false
    }
  });

  setupWindow.loadFile(path.join(__dirname, 'setup.html'));

  setupWindow.on('closed', () => {
    setupWindow = null;
    if (!CONFIG.setupComplete) {
      app.quit();
    }
  });
}

// Download-Funktion mit Fortschrittsanzeige
function downloadFile(url, destPath, onProgress) {
  return new Promise((resolve, reject) => {
    const file = fs.createWriteStream(destPath);

    const request = (url.startsWith('https') ? https : http).get(url, (response) => {
      // Handle redirects
      if (response.statusCode === 301 || response.statusCode === 302) {
        file.close();
        fs.unlinkSync(destPath);
        return downloadFile(response.headers.location, destPath, onProgress)
          .then(resolve)
          .catch(reject);
      }

      const totalSize = parseInt(response.headers['content-length'], 10);
      let downloadedSize = 0;

      response.on('data', (chunk) => {
        downloadedSize += chunk.length;
        if (totalSize && onProgress) {
          const percent = Math.round((downloadedSize / totalSize) * 100);
          onProgress(percent, downloadedSize, totalSize);
        }
      });

      response.pipe(file);

      file.on('finish', () => {
        file.close();
        resolve(destPath);
      });
    });

    request.on('error', (err) => {
      fs.unlink(destPath, () => {});
      reject(err);
    });
  });
}

// Python installieren
async function installPython() {
  const pythonDir = path.join(INSTALL_PATH, 'python');
  const zipPath = path.join(INSTALL_PATH, 'python.zip');

  // Verzeichnis erstellen
  if (!fs.existsSync(INSTALL_PATH)) {
    fs.mkdirSync(INSTALL_PATH, { recursive: true });
  }

  // Progress senden
  const sendProgress = (progress, status, log, type = '') => {
    if (setupWindow) {
      setupWindow.webContents.send('setup-progress', {
        step: 'python',
        progress,
        status,
        log,
        type
      });
    }
  };

  try {
    sendProgress(0, 'Downloading Python...', 'Starting Python download...');

    // Python herunterladen
    await downloadFile(PYTHON_URL, zipPath, (percent) => {
      sendProgress(percent * 0.5, `Downloading... ${percent}%`);
    });

    sendProgress(50, 'Extracting...', 'Download complete. Extracting...', 'success');

    // Entpacken
    await extract(zipPath, { dir: pythonDir });

    sendProgress(60, 'Configuring...', 'Configuring Python...', 'info');

    // Python für pip konfigurieren (._pth Datei bearbeiten)
    const pthFiles = fs.readdirSync(pythonDir).filter(f => f.endsWith('._pth'));
    if (pthFiles.length > 0) {
      const pthPath = path.join(pythonDir, pthFiles[0]);
      let content = fs.readFileSync(pthPath, 'utf8');
      // Uncomment import site
      content = content.replace('#import site', 'import site');
      fs.writeFileSync(pthPath, content);
    }

    sendProgress(65, 'Installing pip...', 'Downloading pip installer...', 'info');

    // get-pip.py herunterladen
    const getPipPath = path.join(pythonDir, 'get-pip.py');
    await downloadFile(GET_PIP_URL, getPipPath, () => {});

    sendProgress(70, 'Installing pip...', 'Running pip installer...', 'info');

    // pip installieren
    const pythonExe = path.join(pythonDir, 'python.exe');
    await new Promise((resolve, reject) => {
      const pip = spawn(pythonExe, [getPipPath], { cwd: pythonDir });
      pip.on('close', (code) => {
        if (code === 0) resolve();
        else reject(new Error(`pip installation failed with code ${code}`));
      });
      pip.on('error', reject);
    });

    sendProgress(100, 'Complete!', 'Python installed successfully!', 'success');

    // Aufräumen
    fs.unlinkSync(zipPath);
    fs.unlinkSync(getPipPath);

    // Config aktualisieren
    CONFIG.pythonPath = pythonExe;
    saveConfig();

    if (setupWindow) {
      setupWindow.webContents.send('setup-step-complete', 1);
    }

    return pythonExe;

  } catch (error) {
    sendProgress(0, 'Error!', `Error: ${error.message}`, 'error');
    throw error;
  }
}

// FFmpeg installieren
async function installFFmpeg() {
  const ffmpegDir = path.join(INSTALL_PATH, 'ffmpeg');
  const zipPath = path.join(INSTALL_PATH, 'ffmpeg.zip');

  const sendProgress = (progress, status, log, type = '') => {
    if (setupWindow) {
      setupWindow.webContents.send('setup-progress', {
        step: 'ffmpeg',
        progress,
        status,
        log,
        type
      });
    }
  };

  try {
    sendProgress(0, 'Downloading FFmpeg...', 'Starting FFmpeg download...');

    await downloadFile(FFMPEG_URL, zipPath, (percent) => {
      sendProgress(percent * 0.7, `Downloading... ${percent}%`);
    });

    sendProgress(70, 'Extracting...', 'Download complete. Extracting...', 'success');

    // Entpacken
    if (!fs.existsSync(ffmpegDir)) {
      fs.mkdirSync(ffmpegDir, { recursive: true });
    }
    await extract(zipPath, { dir: ffmpegDir });

    sendProgress(90, 'Configuring...', 'Finding FFmpeg binaries...', 'info');

    // FFmpeg bin Verzeichnis finden (nested in extracted folder)
    let ffmpegBin = null;
    const findBin = (dir) => {
      const entries = fs.readdirSync(dir, { withFileTypes: true });
      for (const entry of entries) {
        if (entry.isDirectory()) {
          const binPath = path.join(dir, entry.name, 'bin');
          if (fs.existsSync(binPath) && fs.existsSync(path.join(binPath, 'ffmpeg.exe'))) {
            return binPath;
          }
          const nested = findBin(path.join(dir, entry.name));
          if (nested) return nested;
        }
      }
      return null;
    };
    ffmpegBin = findBin(ffmpegDir);

    if (!ffmpegBin) {
      throw new Error('FFmpeg binaries not found in extracted archive');
    }

    sendProgress(100, 'Complete!', `FFmpeg installed at: ${ffmpegBin}`, 'success');

    // Aufräumen
    fs.unlinkSync(zipPath);

    // Config aktualisieren
    CONFIG.ffmpegPath = ffmpegBin;
    saveConfig();

    if (setupWindow) {
      setupWindow.webContents.send('setup-step-complete', 2);
    }

    return ffmpegBin;

  } catch (error) {
    sendProgress(0, 'Error!', `Error: ${error.message}`, 'error');
    throw error;
  }
}

// Detect system CUDA version from nvidia-smi
function detectCudaVersion() {
  try {
    const output = execSync('nvidia-smi --query-gpu=driver_version --format=csv,noheader', { encoding: 'utf8', timeout: 10000 });
    // Also get CUDA version from nvidia-smi
    const smiOutput = execSync('nvidia-smi', { encoding: 'utf8', timeout: 10000 });
    const cudaMatch = smiOutput.match(/CUDA Version:\s*(\d+)\.(\d+)/);
    if (cudaMatch) {
      const major = parseInt(cudaMatch[1]);
      const minor = parseInt(cudaMatch[2]);
      console.log(`Detected CUDA version: ${major}.${minor}`);
      return { major, minor };
    }
  } catch (err) {
    console.log('Could not detect CUDA version:', err.message);
  }
  return null;
}

// Check if a PyTorch CUDA index URL exists
function checkPytorchIndexExists(indexUrl) {
  return new Promise((resolve) => {
    // Check the base index URL (e.g., https://download.pytorch.org/whl/cu130)
    https.get(indexUrl, { timeout: 5000 }, (res) => {
      // 200 = exists, 301/302 = redirect (also valid), 404 = not found
      resolve(res.statusCode === 200 || res.statusCode === 301 || res.statusCode === 302);
    }).on('error', () => {
      resolve(false);
    }).on('timeout', () => {
      resolve(false);
    });
  });
}

// Generate list of CUDA index URLs to try, from highest to lowest
function generateCudaIndexCandidates(cudaVersion) {
  const { major, minor } = cudaVersion;
  const candidates = [];

  // Start from detected version and go down
  // Format: cu{major}{minor} - e.g., cu131 for CUDA 13.1, cu130 for 13.0
  for (let m = major; m >= 11; m--) {
    const startMinor = (m === major) ? minor : 9;
    const endMinor = (m === 11) ? 8 : 0; // CUDA 11.8 is minimum supported

    for (let n = startMinor; n >= endMinor; n--) {
      // PyTorch uses specific version tags, not all combinations exist
      // Common pattern: cu{major}{minor} like cu130, cu128, cu126, cu124, cu121, cu118
      const tag = `cu${m}${n}`;
      candidates.push(`https://download.pytorch.org/whl/${tag}`);
    }
  }

  return candidates;
}

// Get best PyTorch CUDA index URL based on system CUDA version (async with validation)
async function getPytorchCudaIndexUrlAsync(cudaVersion) {
  if (!cudaVersion) {
    return null;
  }

  const candidates = generateCudaIndexCandidates(cudaVersion);
  console.log(`Checking PyTorch CUDA indexes for CUDA ${cudaVersion.major}.${cudaVersion.minor}...`);

  for (const indexUrl of candidates) {
    const tag = indexUrl.split('/').pop();
    const exists = await checkPytorchIndexExists(indexUrl);
    if (exists) {
      console.log(`Found available PyTorch index: ${tag}`);
      return indexUrl;
    }
    console.log(`Index ${tag} not available, trying next...`);
  }

  // Fallback to cu118 as minimum known version
  console.log('No matching index found, falling back to cu118');
  return 'https://download.pytorch.org/whl/cu118';
}

// Synchronous version for backward compatibility (uses known versions)
function getPytorchCudaIndexUrl(cudaVersion) {
  if (!cudaVersion) {
    return null;
  }

  const { major, minor } = cudaVersion;

  // Known available PyTorch CUDA builds (fallback list)
  // This is used when async check isn't possible
  if (major >= 13) {
    return 'https://download.pytorch.org/whl/cu130';
  } else if (major === 12 && minor >= 8) {
    return 'https://download.pytorch.org/whl/cu128';
  } else if (major === 12 && minor >= 6) {
    return 'https://download.pytorch.org/whl/cu126';
  } else if (major === 12 && minor >= 4) {
    return 'https://download.pytorch.org/whl/cu124';
  } else if (major === 12 && minor >= 1) {
    return 'https://download.pytorch.org/whl/cu121';
  } else if (major === 11 && minor >= 8) {
    return 'https://download.pytorch.org/whl/cu118';
  } else {
    return 'https://download.pytorch.org/whl/cu118';
  }
}

// Whisper installieren
async function installWhisper(useCuda = false) {
  const pythonPath = getPythonPath();
  if (!pythonPath) {
    throw new Error('Python not found');
  }

  const sendProgress = (progress, status, log, type = '') => {
    if (setupWindow) {
      setupWindow.webContents.send('setup-progress', {
        step: 'whisper',
        progress,
        status,
        log,
        type
      });
    }
  };

  try {
    const pythonDir = path.dirname(pythonPath);
    const pipPath = path.join(pythonDir, 'Scripts', 'pip.exe');

    sendProgress(5, 'Installing dependencies...', 'Installing OpenAI Whisper...', 'info');

    // PyTorch installieren (CPU oder CUDA)
    if (useCuda) {
      // Auto-detect CUDA version and dynamically find best PyTorch build
      const cudaVersion = detectCudaVersion();

      sendProgress(8, 'Checking PyTorch versions...', `Detected CUDA ${cudaVersion ? cudaVersion.major + '.' + cudaVersion.minor : 'N/A'}, finding best PyTorch build...`, 'info');

      // Use async version to dynamically check available indexes
      const indexUrl = await getPytorchCudaIndexUrlAsync(cudaVersion);
      const cudaLabel = indexUrl ? indexUrl.split('/').pop() : 'CPU';

      sendProgress(10, `Installing PyTorch (${cudaLabel})...`, `Using PyTorch ${cudaLabel} build...`, 'info');

      if (indexUrl) {
        await runPip(pipPath, ['install', 'torch', 'torchvision', 'torchaudio', '--index-url', indexUrl], sendProgress);
      } else {
        // Fallback to CPU if no CUDA detected
        await runPip(pipPath, ['install', 'torch', 'torchvision', 'torchaudio'], sendProgress);
      }
    }

    // Whisper installieren
    sendProgress(50, 'Installing Whisper...', 'Installing openai-whisper...', 'info');
    await runPip(pipPath, ['install', 'openai-whisper'], sendProgress);

    sendProgress(100, 'Complete!', 'Whisper installed successfully!', 'success');

    CONFIG.cudaEnabled = useCuda;
    saveConfig();

    if (setupWindow) {
      setupWindow.webContents.send('setup-step-complete', 3);
    }

  } catch (error) {
    sendProgress(0, 'Error!', `Error: ${error.message}`, 'error');
    throw error;
  }
}

// pip Befehl ausführen
function runPip(pipPath, args, sendProgress) {
  return new Promise((resolve, reject) => {
    const pip = spawn(pipPath, args, {
      env: { ...process.env, PYTHONIOENCODING: 'utf-8' }
    });

    pip.stdout.on('data', (data) => {
      const line = data.toString().trim();
      if (line && sendProgress) {
        sendProgress(null, null, line, 'info');
      }
    });

    pip.stderr.on('data', (data) => {
      const line = data.toString().trim();
      if (line && sendProgress) {
        sendProgress(null, null, line, 'info');
      }
    });

    pip.on('close', (code) => {
      if (code === 0) resolve();
      else reject(new Error(`pip exited with code ${code}`));
    });

    pip.on('error', reject);
  });
}

// Setup abschließen
function completeSetup(selectedModel) {
  CONFIG.setupComplete = true;
  if (selectedModel) {
    CONFIG.whisperModel = selectedModel;
  }
  saveConfig();
}

// ============================================
// HAUPT-FENSTER
// ============================================

function createMainWindow() {
  mainWindow = new BrowserWindow({
    width: 700,
    height: 700,
    resizable: true,
    frame: false,
    show: false,  // Startet versteckt
    title: 'Whisper Voice',
    icon: getIconPath(),
    webPreferences: {
      nodeIntegration: true,
      contextIsolation: false
    }
  });

  mainWindow.loadFile(path.join(__dirname, 'index.html'));

  if (process.argv.includes('--dev')) {
    mainWindow.webContents.openDevTools();
  }

  mainWindow.on('close', (event) => {
    if (!app.isQuitting) {
      event.preventDefault();
      mainWindow.hide();
    }
  });
}

function createFloatingBar() {
  const { width, height } = screen.getPrimaryDisplay().workAreaSize;

  floatingBar = new BrowserWindow({
    width: 280,
    height: 46,
    x: Math.round((width - 280) / 2),
    y: height - 46 - 40,
    frame: false,
    transparent: true,
    alwaysOnTop: true,
    skipTaskbar: true,
    resizable: false,
    focusable: false,
    webPreferences: {
      nodeIntegration: true,
      contextIsolation: false
    }
  });

  floatingBar.loadFile(path.join(__dirname, 'floatingBar.html'));
  floatingBar.setIgnoreMouseEvents(false);
  floatingBar.setMenu(null);

  // System-Kontextmenü bei Rechtsklick auf Drag-Bereich verhindern
  floatingBar.hookWindowMessage(0x0116, () => {
    floatingBar.setEnabled(false);
    floatingBar.setEnabled(true);
    // Custom Menü zeigen
    const contextMenu = Menu.buildFromTemplate([
      { label: 'Whisper Voice', enabled: false },
      { type: 'separator' },
      { label: 'Hauptfenster öffnen', click: () => { mainWindow.show(); mainWindow.focus(); } },
      {
        label: 'Floating Bar ausblenden',
        click: () => {
          CONFIG.showFloatingBar = false;
          floatingBar.hide();
          updateTrayMenu();
          saveConfig();
        }
      },
      { type: 'separator' },
      { label: `Hotkey: ${CONFIG.hotkeyDisplay}`, enabled: false },
      { label: `Modell: ${CONFIG.whisperModel}`, enabled: false },
      { type: 'separator' },
      { label: 'Beenden', click: () => { app.isQuitting = true; app.quit(); } }
    ]);
    contextMenu.popup();
    return true;
  });

  // Hotkey anzeigen
  floatingBar.webContents.on('did-finish-load', () => {
    floatingBar.webContents.send('set-hotkey', CONFIG.hotkeyDisplay);
  });
}

function createTray() {
  const icon = nativeImage.createFromPath(getIconPath()).resize({ width: 16, height: 16 });
  tray = new Tray(icon);

  updateTrayMenu();

  tray.setToolTip('Whisper Voice - Lädt...');

  tray.on('click', () => {
    if (mainWindow.isVisible()) {
      mainWindow.hide();
    } else {
      mainWindow.show();
      mainWindow.focus();
    }
  });
}

function updateTrayMenu() {
  const contextMenu = Menu.buildFromTemplate([
    { label: 'Whisper Voice', enabled: false },
    { type: 'separator' },
    { label: 'Hauptfenster öffnen', click: () => { mainWindow.show(); mainWindow.focus(); } },
    {
      label: CONFIG.showFloatingBar ? 'Floating Bar ausblenden' : 'Floating Bar einblenden',
      click: () => {
        CONFIG.showFloatingBar = !CONFIG.showFloatingBar;
        if (CONFIG.showFloatingBar) {
          floatingBar.show();
        } else {
          floatingBar.hide();
        }
        updateTrayMenu();
      }
    },
    { type: 'separator' },
    { label: `Hotkey: ${CONFIG.hotkeyDisplay}`, enabled: false },
    { label: `Modell: ${CONFIG.whisperModel}`, enabled: false },
    { type: 'separator' },
    { label: 'Beenden', click: () => { app.isQuitting = true; app.quit(); } }
  ]);

  tray.setContextMenu(contextMenu);
}

function startWhisperServer() {
  return new Promise((resolve, reject) => {
    // In packaged app, scripts are in resources/scripts/
    const serverScript = app.isPackaged
      ? path.join(process.resourcesPath, 'scripts', 'whisper_server.py')
      : path.join(__dirname, '..', 'scripts', 'whisper_server.py');
    const pythonPath = getPythonPath();

    if (!pythonPath) {
      console.error('Python nicht gefunden!');
      reject(new Error('Python not found'));
      return;
    }

    console.log('Starte Whisper Server...');
    console.log('Python:', pythonPath);

    // Send loading status to renderer
    const sendLoadingStatus = (status, progress) => {
      if (mainWindow && !mainWindow.isDestroyed()) {
        mainWindow.webContents.send('loading-status', { status, progress });
      }
    };

    sendLoadingStatus('Starte Python Server...', 20);

    // Umgebungsvariablen für FFmpeg
    const env = { ...process.env };
    if (CONFIG.ffmpegPath) {
      env.FFMPEG_PATH = CONFIG.ffmpegPath;
      env.PATH = CONFIG.ffmpegPath + ';' + env.PATH;
    }

    whisperServer = spawn(pythonPath, [serverScript, SERVER_PORT.toString(), CONFIG.whisperModel], {
      stdio: ['pipe', 'pipe', 'pipe'],
      env: env
    });

    whisperServer.stderr.on('data', (data) => {
      const msg = data.toString().trim();
      console.log('Whisper:', msg);

      // Update loading status based on server output
      if (msg.includes('Lade Whisper Modell')) {
        sendLoadingStatus(`Lade Whisper Modell '${CONFIG.whisperModel}'...`, 40);
      } else if (msg.includes('GPU erkannt')) {
        sendLoadingStatus('GPU erkannt, initialisiere CUDA...', 60);
      } else if (msg.includes('Modell geladen')) {
        sendLoadingStatus('Modell geladen, starte Server...', 80);
      } else if (msg.includes('Server bereit')) {
        sendLoadingStatus('Bereit!', 100);
        tray.setToolTip('Whisper Voice - Bereit');
        updateFloatingBar('ready', `${CONFIG.hotkeyDisplay} zum Diktieren`);
        // Small delay to show 100% before hiding
        setTimeout(() => {
          if (mainWindow && !mainWindow.isDestroyed()) {
            mainWindow.webContents.send('server-ready');
          }
          resolve();
        }, 500);
      }
    });

    whisperServer.on('close', (code) => {
      console.log('Server beendet:', code);
      whisperServer = null;
    });

    whisperServer.on('error', (err) => {
      console.error('Server Fehler:', err);
      sendLoadingStatus('Fehler beim Starten!', 0);
      reject(err);
    });

    // Timeout fallback - resolve anyway after 60 seconds
    setTimeout(() => {
      sendLoadingStatus('Server gestartet (Timeout)', 100);
      if (mainWindow && !mainWindow.isDestroyed()) {
        mainWindow.webContents.send('server-ready');
      }
      resolve();
    }, 60000);
  });
}

function stopWhisperServer() {
  if (whisperServer) {
    whisperServer.kill();
    whisperServer = null;
  }
}

// ==================== HELPER APP FÜR WIN-KEY ====================

function getVirtualKeyCode(key) {
  const vkMap = {
    'A': 0x41, 'B': 0x42, 'C': 0x43, 'D': 0x44, 'E': 0x45, 'F': 0x46,
    'G': 0x47, 'H': 0x48, 'I': 0x49, 'J': 0x4A, 'K': 0x4B, 'L': 0x4C,
    'M': 0x4D, 'N': 0x4E, 'O': 0x4F, 'P': 0x50, 'Q': 0x51, 'R': 0x52,
    'S': 0x53, 'T': 0x54, 'U': 0x55, 'V': 0x56, 'W': 0x57, 'X': 0x58,
    'Y': 0x59, 'Z': 0x5A,
    '0': 0x30, '1': 0x31, '2': 0x32, '3': 0x33, '4': 0x34,
    '5': 0x35, '6': 0x36, '7': 0x37, '8': 0x38, '9': 0x39,
    ' ': 0x20, 'SPACE': 0x20
  };
  return vkMap[key.toUpperCase()] || 0x44; // Default: D
}

function startHelperServer() {
  return new Promise((resolve) => {
    helperServer = net.createServer((socket) => {
      console.log('Helper verbunden!');
      helperSocket = socket;

      // Hotkey an Helper senden (Format: ctrl,shift,alt,win,vkcode)
      sendHotkeyToHelper();

      socket.on('data', (data) => {
        const messages = data.toString().trim().split('\n');
        messages.forEach((msg) => {
          if (msg === 'keydown') {
            if (CONFIG.toggleMode) {
              // Toggle-Modus: Antippen wechselt zwischen Start/Stop
              if (isRecording) {
                console.log('Hotkey gedrückt - Toggle Stop (von Helper)');
                stopRecording();
              } else {
                console.log('Hotkey gedrückt - Toggle Start (von Helper)');
                startRecording();
              }
            } else if (!isRecording) {
              // Push-to-Talk: Nur starten wenn nicht aufnimmt
              console.log('Hotkey gedrückt (von Helper)');
              startRecording();
            }
          } else if (msg === 'keyup' && isRecording && !CONFIG.toggleMode) {
            // Push-to-Talk: Stoppen beim Loslassen
            console.log('Hotkey losgelassen (von Helper)');
            stopRecording();
          } else if (msg === 'connected') {
            console.log('Helper bereit');
          } else if (msg.startsWith('recorded:')) {
            // Format: recorded:ctrl,shift,alt,win,vkcode,displayname
            const parts = msg.substring(9).split(',');
            if (parts.length >= 6) {
              const hotkeyConfig = {
                ctrl: parts[0] === '1',
                shift: parts[1] === '1',
                alt: parts[2] === '1',
                win: parts[3] === '1',
                key: String.fromCharCode(parseInt(parts[4])),
                vkCode: parseInt(parts[4]),
                display: parts.slice(5).join(',')
              };
              console.log('Hotkey aufgenommen:', hotkeyConfig.display);

              // Config aktualisieren
              CONFIG.hotkey = {
                ctrl: hotkeyConfig.ctrl,
                shift: hotkeyConfig.shift,
                alt: hotkeyConfig.alt,
                win: hotkeyConfig.win,
                key: hotkeyConfig.key,
                vkCode: hotkeyConfig.vkCode  // Für modifier-only (vkCode=0)
              };
              CONFIG.hotkeyDisplay = hotkeyConfig.display + (CONFIG.toggleMode ? ' (tippen)' : ' (halten)');

              // Helper aktualisieren
              sendHotkeyToHelper();

              // UI aktualisieren
              if (mainWindow && !mainWindow.isDestroyed()) {
                mainWindow.webContents.send('hotkey-recorded', hotkeyConfig);
              }
              if (floatingBar && !floatingBar.isDestroyed()) {
                floatingBar.webContents.send('set-hotkey', CONFIG.hotkeyDisplay);
              }

              updateTrayMenu();
              saveConfig();
            }
          }
        });
      });

      socket.on('close', () => {
        console.log('Helper getrennt');
        helperSocket = null;
      });

      socket.on('error', () => {});
    });

    helperServer.listen(HELPER_PORT, '127.0.0.1', () => {
      console.log(`Helper-Server lauscht auf Port ${HELPER_PORT}`);
      resolve();
    });

    helperServer.on('error', (err) => {
      console.error('Helper-Server Fehler:', err.message);
      resolve();
    });
  });
}

function sendHotkeyToHelper() {
  if (helperSocket) {
    // Verwende vkCode direkt wenn vorhanden (für modifier-only), sonst aus key berechnen
    const vk = CONFIG.hotkey.vkCode !== undefined ? CONFIG.hotkey.vkCode : getVirtualKeyCode(CONFIG.hotkey.key);
    const ctrl = CONFIG.hotkey.ctrl ? '1' : '0';
    const shift = CONFIG.hotkey.shift ? '1' : '0';
    const alt = CONFIG.hotkey.alt ? '1' : '0';
    const win = CONFIG.hotkey.win ? '1' : '0';
    helperSocket.write(`setkey:${ctrl},${shift},${alt},${win},${vk}\n`);
  }
}

function startHelperProcess() {
  // In packaged app, bin is in resources/bin/
  const helperPath = app.isPackaged
    ? path.join(process.resourcesPath, 'bin', 'WhisperHelper.exe')
    : path.join(__dirname, '..', 'bin', 'WhisperHelper.exe');

  if (!fs.existsSync(helperPath)) {
    console.log('Helper nicht gefunden:', helperPath);
    console.log('Bitte erst builden: cd helper && build.bat');
    return;
  }

  const vk = getVirtualKeyCode(CONFIG.hotkey.key);
  console.log(`Starte Helper für Win+${CONFIG.hotkey.key} (VK: ${vk})...`);

  helperProcess = spawn(helperPath, [vk.toString()], {
    stdio: 'ignore',
    detached: false,
    windowsHide: true
  });

  helperProcess.on('close', (code) => {
    console.log('Helper beendet:', code);
    helperProcess = null;
  });

  helperProcess.on('error', (err) => {
    console.error('Helper Fehler:', err.message);
  });
}

function stopHelper() {
  if (helperSocket) {
    try {
      helperSocket.write('quit\n');
    } catch (e) {}
  }
  if (helperProcess) {
    helperProcess.kill();
    helperProcess = null;
  }
  if (helperServer) {
    helperServer.close();
    helperServer = null;
  }
}

function updateHelperHotkey() {
  if (helperSocket && CONFIG.hotkey.win) {
    const vk = getVirtualKeyCode(CONFIG.hotkey.key);
    helperSocket.write(`setkey:${vk}\n`);
  }
}

// ==================== UIOHOOK FÜR ANDERE KEYS ====================

// Mapping von Buchstaben zu UiohookKey codes
function getKeyCode(key) {
  const keyMap = {
    'A': UiohookKey.A, 'B': UiohookKey.B, 'C': UiohookKey.C, 'D': UiohookKey.D,
    'E': UiohookKey.E, 'F': UiohookKey.F, 'G': UiohookKey.G, 'H': UiohookKey.H,
    'I': UiohookKey.I, 'J': UiohookKey.J, 'K': UiohookKey.K, 'L': UiohookKey.L,
    'M': UiohookKey.M, 'N': UiohookKey.N, 'O': UiohookKey.O, 'P': UiohookKey.P,
    'Q': UiohookKey.Q, 'R': UiohookKey.R, 'S': UiohookKey.S, 'T': UiohookKey.T,
    'U': UiohookKey.U, 'V': UiohookKey.V, 'W': UiohookKey.W, 'X': UiohookKey.X,
    'Y': UiohookKey.Y, 'Z': UiohookKey.Z,
    '0': UiohookKey.Num0, '1': UiohookKey.Num1, '2': UiohookKey.Num2,
    '3': UiohookKey.Num3, '4': UiohookKey.Num4, '5': UiohookKey.Num5,
    '6': UiohookKey.Num6, '7': UiohookKey.Num7, '8': UiohookKey.Num8,
    '9': UiohookKey.Num9,
    'F1': UiohookKey.F1, 'F2': UiohookKey.F2, 'F3': UiohookKey.F3,
    'F4': UiohookKey.F4, 'F5': UiohookKey.F5, 'F6': UiohookKey.F6,
    'F7': UiohookKey.F7, 'F8': UiohookKey.F8, 'F9': UiohookKey.F9,
    'F10': UiohookKey.F10, 'F11': UiohookKey.F11, 'F12': UiohookKey.F12,
    ' ': UiohookKey.Space, 'SPACE': UiohookKey.Space
  };
  return keyMap[key.toUpperCase()] || null;
}

function setupPushToTalk() {
  // Key codes für uiohook-napi
  const KEY_CTRL_LEFT = UiohookKey.Ctrl;
  const KEY_CTRL_RIGHT = UiohookKey.CtrlRight;
  const KEY_SHIFT_LEFT = UiohookKey.Shift;
  const KEY_SHIFT_RIGHT = UiohookKey.ShiftRight;
  const KEY_ALT_LEFT = UiohookKey.Alt;
  const KEY_ALT_RIGHT = UiohookKey.AltRight;

  uIOhook.on('keydown', (e) => {
    // Ctrl gedrückt
    if (e.keycode === KEY_CTRL_LEFT || e.keycode === KEY_CTRL_RIGHT) {
      keysPressed.ctrl = true;
    }
    // Shift gedrückt
    if (e.keycode === KEY_SHIFT_LEFT || e.keycode === KEY_SHIFT_RIGHT) {
      keysPressed.shift = true;
    }
    // Alt gedrückt
    if (e.keycode === KEY_ALT_LEFT || e.keycode === KEY_ALT_RIGHT) {
      keysPressed.alt = true;
    }
    // Konfigurierte Taste gedrückt
    const configuredKeyCode = getKeyCode(CONFIG.hotkey.key);
    if (configuredKeyCode && e.keycode === configuredKeyCode) {
      keysPressed.key = true;
    }

    // Prüfe ob alle konfigurierten Tasten gedrückt sind
    const allPressed =
      (!CONFIG.hotkey.ctrl || keysPressed.ctrl) &&
      (!CONFIG.hotkey.shift || keysPressed.shift) &&
      (!CONFIG.hotkey.alt || keysPressed.alt) &&
      keysPressed.key;

    if (allPressed && !hotkeyTriggered) {
      hotkeyTriggered = true;  // Markiere als ausgelöst
      if (CONFIG.toggleMode) {
        // Toggle-Modus: Wechseln zwischen Start/Stop
        if (isRecording) {
          stopRecording();
        } else {
          startRecording();
        }
      } else if (!isRecording) {
        // Push-to-Talk: Nur starten
        startRecording();
      }
    }
  });

  uIOhook.on('keyup', (e) => {
    // Ctrl losgelassen
    if (e.keycode === KEY_CTRL_LEFT || e.keycode === KEY_CTRL_RIGHT) {
      keysPressed.ctrl = false;
    }
    // Shift losgelassen
    if (e.keycode === KEY_SHIFT_LEFT || e.keycode === KEY_SHIFT_RIGHT) {
      keysPressed.shift = false;
    }
    // Alt losgelassen
    if (e.keycode === KEY_ALT_LEFT || e.keycode === KEY_ALT_RIGHT) {
      keysPressed.alt = false;
    }
    // Konfigurierte Taste losgelassen
    const configuredKeyCode = getKeyCode(CONFIG.hotkey.key);
    if (configuredKeyCode && e.keycode === configuredKeyCode) {
      keysPressed.key = false;
    }

    // Prüfe ob eine benötigte Taste losgelassen wurde
    const missingKey =
      (CONFIG.hotkey.ctrl && !keysPressed.ctrl) ||
      (CONFIG.hotkey.shift && !keysPressed.shift) ||
      (CONFIG.hotkey.alt && !keysPressed.alt) ||
      !keysPressed.key;

    // Hotkey-Trigger zurücksetzen wenn Taste losgelassen
    if (missingKey) {
      hotkeyTriggered = false;
    }

    // Push-to-Talk: Stoppen beim Loslassen (nicht im Toggle-Modus)
    if (CONFIG.toggleMode) return;

    if (isRecording && missingKey) {
      stopRecording();
    }
  });

  uIOhook.start();
  console.log(`Push-to-Talk aktiv: ${CONFIG.hotkeyDisplay}`);
}

function startRecording() {
  if (isRecording) return;
  isRecording = true;
  console.log('Aufnahme gestartet');
  updateFloatingBar('recording', 'Aufnahme...');
  mainWindow.webContents.send('start-recording');
}

function stopRecording() {
  if (!isRecording) return;
  isRecording = false;
  console.log('Aufnahme gestoppt');
  updateFloatingBar('processing', 'Verarbeite...');
  mainWindow.webContents.send('stop-recording');
}

function updateFloatingBar(status, text) {
  if (floatingBar && !floatingBar.isDestroyed()) {
    floatingBar.webContents.send('status-update', { status, text });
  }
}

function transcribeViaServer(audioPath) {
  return new Promise((resolve, reject) => {
    const data = JSON.stringify({
      audio_path: audioPath,
      model: CONFIG.whisperModel,
      language: CONFIG.language
    });

    const req = http.request({
      hostname: '127.0.0.1',
      port: SERVER_PORT,
      path: '/transcribe',
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Content-Length': data.length
      }
    }, (res) => {
      let body = '';
      res.on('data', chunk => body += chunk);
      res.on('end', () => {
        try {
          const result = JSON.parse(body);
          result.error ? reject(new Error(result.error)) : resolve(result.text);
        } catch (e) {
          reject(e);
        }
      });
    });

    req.on('error', reject);
    req.write(data);
    req.end();
  });
}

// IPC Handlers
ipcMain.handle('get-config', () => CONFIG);

ipcMain.handle('transcribe', async (event, audioPath, options = {}) => {
  try {
    const text = await transcribeViaServer(audioPath);
    const skipAutoPaste = options.skipAutoPaste || false;

    clipboard.writeText(text);
    console.log('Text:', text);

    if (CONFIG.autoPaste && !skipAutoPaste) {
      setTimeout(() => {
        const { exec } = require('child_process');
        exec('powershell -command "$wshell = New-Object -ComObject wscript.shell; $wshell.SendKeys(\'^v\')"');
      }, 100);
    }

    isRecording = false;
    updateFloatingBar('ready', `${CONFIG.hotkeyDisplay} zum Diktieren`);
    tray.setToolTip('Whisper Voice - Bereit');

    return text;
  } catch (err) {
    console.error('Fehler:', err);
    isRecording = false;
    updateFloatingBar('ready', 'Fehler - Erneut versuchen');
    throw err;
  }
});

ipcMain.handle('update-config', (event, newConfig) => {
  const toggleModeChanged = newConfig.toggleMode !== undefined && newConfig.toggleMode !== CONFIG.toggleMode;
  const floatingBarChanged = newConfig.showFloatingBar !== undefined && newConfig.showFloatingBar !== CONFIG.showFloatingBar;
  Object.assign(CONFIG, newConfig);

  // hotkeyDisplay aktualisieren wenn toggleMode geändert wurde
  if (toggleModeChanged) {
    const baseDisplay = CONFIG.hotkeyDisplay.replace(' (halten)', '').replace(' (tippen)', '');
    CONFIG.hotkeyDisplay = baseDisplay + (CONFIG.toggleMode ? ' (tippen)' : ' (halten)');
    if (floatingBar && !floatingBar.isDestroyed()) {
      floatingBar.webContents.send('set-hotkey', CONFIG.hotkeyDisplay);
    }
  }

  // Floating Bar ein-/ausblenden wenn Einstellung geändert wurde
  if (floatingBarChanged && floatingBar && !floatingBar.isDestroyed()) {
    if (CONFIG.showFloatingBar) {
      floatingBar.show();
    } else {
      floatingBar.hide();
    }
  }

  updateTrayMenu();
  saveConfig();
  return CONFIG;
});

ipcMain.handle('set-hotkey', (event, hotkeyConfig) => {
  CONFIG.hotkey = {
    ctrl: hotkeyConfig.ctrl,
    shift: hotkeyConfig.shift,
    alt: hotkeyConfig.alt,
    win: hotkeyConfig.win || false,
    key: hotkeyConfig.key
  };
  CONFIG.hotkeyDisplay = hotkeyConfig.display + (CONFIG.toggleMode ? ' (tippen)' : ' (halten)');

  // Floating Bar aktualisieren
  if (floatingBar && !floatingBar.isDestroyed()) {
    floatingBar.webContents.send('set-hotkey', CONFIG.hotkeyDisplay);
  }

  // Helper aktualisieren wenn Win-Key verwendet
  updateHelperHotkey();

  updateTrayMenu();
  saveConfig();
  console.log(`Neuer Hotkey: ${CONFIG.hotkeyDisplay}`);
  return CONFIG;
});

ipcMain.on('recording-complete', () => {
  isRecording = false;
  updateFloatingBar('ready', `${CONFIG.hotkeyDisplay} zum Diktieren`);
});

// ============================================
// SYSTEM INFO & GPU SETTINGS
// ============================================

// Get system info from Python
ipcMain.handle('get-system-info', async () => {
  const pythonPath = getPythonPath();
  if (!pythonPath) {
    return {
      pytorchVersion: 'Python nicht gefunden',
      whisperVersion: null,
      cudaVersion: null,
      cudaAvailable: false,
      gpuName: null,
      gpuCompatible: false,
      gpuEnabled: CONFIG.cudaEnabled,
      activeDevice: 'cpu'
    };
  }

  return new Promise((resolve) => {
    const script = `
import json
import sys
import warnings
warnings.filterwarnings("ignore")

try:
    import torch

    # Get Whisper version
    whisper_version = None
    try:
        import whisper
        whisper_version = getattr(whisper, '__version__', None)
        if not whisper_version:
            # Try to get from package metadata
            try:
                from importlib.metadata import version
                whisper_version = version('openai-whisper')
            except:
                whisper_version = 'Installiert'
    except ImportError:
        whisper_version = 'Nicht installiert'

    info = {
        'pytorchVersion': torch.__version__,
        'whisperVersion': whisper_version,
        'cudaVersion': torch.version.cuda if torch.cuda.is_available() else None,
        'cudaAvailable': torch.cuda.is_available(),
        'gpuName': torch.cuda.get_device_name(0) if torch.cuda.is_available() else None,
        'gpuCompatible': False,
        'activeDevice': 'cpu'
    }
    # Test GPU compatibility with actual CUDA operation
    if torch.cuda.is_available():
        try:
            # Simple tensor test might pass, but actual operations fail on unsupported GPUs
            # Test with matrix multiplication which requires CUDA kernels
            x = torch.randn(10, 10, device='cuda')
            y = torch.randn(10, 10, device='cuda')
            z = torch.mm(x, y)  # Matrix multiply - this triggers kernel execution
            torch.cuda.synchronize()  # Wait for kernel to complete
            del x, y, z
            torch.cuda.empty_cache()
            info['gpuCompatible'] = True
        except RuntimeError as e:
            err_str = str(e).lower()
            if 'no kernel image' in err_str or 'cuda error' in err_str or 'not compatible' in err_str:
                info['gpuCompatible'] = False
            else:
                info['gpuCompatible'] = False
    print(json.dumps(info))
except ImportError:
    print(json.dumps({'pytorchVersion': 'Nicht installiert', 'whisperVersion': None, 'cudaAvailable': False, 'gpuCompatible': False}))
except Exception as e:
    print(json.dumps({'pytorchVersion': 'Fehler: ' + str(e)[:50], 'whisperVersion': None, 'cudaAvailable': False, 'gpuCompatible': False}))
`;

    const python = spawn(pythonPath, ['-c', script], { timeout: 30000 });
    let output = '';

    python.stdout.on('data', (data) => {
      output += data.toString();
    });

    python.on('close', async () => {
      try {
        const info = JSON.parse(output.trim());
        info.gpuEnabled = CONFIG.cudaEnabled;

        // Get actual active device from whisper server health endpoint
        try {
          const healthResponse = await new Promise((resolveHealth, rejectHealth) => {
            const req = http.request({
              hostname: '127.0.0.1',
              port: SERVER_PORT,
              path: '/health',
              method: 'GET',
              timeout: 2000
            }, (res) => {
              let data = '';
              res.on('data', chunk => data += chunk);
              res.on('end', () => {
                try {
                  resolveHealth(JSON.parse(data));
                } catch {
                  resolveHealth(null);
                }
              });
            });
            req.on('error', () => resolveHealth(null));
            req.on('timeout', () => { req.destroy(); resolveHealth(null); });
            req.end();
          });

          if (healthResponse && healthResponse.device) {
            info.activeDevice = healthResponse.device.includes('cuda') ? 'GPU' : 'CPU';
          } else {
            // Server not responding - show expected mode based on config
            info.activeDevice = CONFIG.cudaEnabled && info.gpuCompatible ? 'GPU (erwartet)' : 'CPU';
          }
        } catch {
          // Fallback to expected mode based on config
          info.activeDevice = CONFIG.cudaEnabled && info.gpuCompatible ? 'GPU (erwartet)' : 'CPU';
        }

        resolve(info);
      } catch (e) {
        resolve({
          pytorchVersion: 'Fehler beim Laden',
          cudaVersion: null,
          cudaAvailable: false,
          gpuName: null,
          gpuCompatible: false,
          gpuEnabled: CONFIG.cudaEnabled,
          activeDevice: 'cpu'
        });
      }
    });

    python.on('error', () => {
      resolve({
        pytorchVersion: 'Python-Fehler',
        cudaVersion: null,
        cudaAvailable: false,
        gpuName: null,
        gpuCompatible: false,
        gpuEnabled: CONFIG.cudaEnabled,
        activeDevice: 'cpu'
      });
    });
  });
});

// Set GPU mode
ipcMain.handle('set-gpu-mode', async (event, enabled) => {
  CONFIG.cudaEnabled = enabled;
  saveConfig();
  return { success: true };
});

// Update PyTorch
ipcMain.handle('update-pytorch', async (event, options = {}) => {
  const pythonPath = getPythonPath();
  if (!pythonPath) {
    return { success: false, message: 'Python nicht gefunden' };
  }

  const pipPath = path.join(path.dirname(pythonPath), 'Scripts', 'pip.exe');

  // Auto-detect CUDA if no indexUrl specified
  let indexUrl = options.indexUrl;
  if (indexUrl === undefined || indexUrl === '') {
    const cudaVersion = detectCudaVersion();
    indexUrl = await getPytorchCudaIndexUrlAsync(cudaVersion);
    console.log('Auto-detected CUDA, using index:', indexUrl);
  }

  return new Promise((resolve) => {
    // First uninstall old version
    console.log('Uninstalling old PyTorch...');
    const uninstall = spawn(pipPath, ['uninstall', 'torch', 'torchvision', 'torchaudio', '-y'], { timeout: 60000 });

    uninstall.on('close', () => {
      // Build install arguments based on selected index URL
      let args = ['install'];

      // Add --pre flag for nightly builds
      if (indexUrl && indexUrl.includes('nightly')) {
        args.push('--pre');
      }

      args.push('torch', 'torchvision', 'torchaudio');

      // Add index URL if specified (not CPU-only)
      if (indexUrl) {
        args.push('--index-url', indexUrl);
      }

      console.log('Installing PyTorch with args:', args.join(' '));
      const pip = spawn(pipPath, args, { timeout: 600000 });
      let output = '';

      pip.stdout.on('data', (data) => { output += data.toString(); });
      pip.stderr.on('data', (data) => { output += data.toString(); });

      pip.on('close', (code) => {
        console.log('PyTorch install exit code:', code);
        console.log('PyTorch install output (last 500 chars):', output.slice(-500));

        if (code === 0 || output.includes('Successfully installed')) {
          const cudaInfo = indexUrl ? indexUrl.split('/').pop() : 'CPU';
          resolve({ success: true, message: `PyTorch (${cudaInfo}) erfolgreich installiert! Neustart erforderlich.` });
        } else if (output.includes('No matching distribution')) {
          resolve({ success: false, message: 'Diese PyTorch-Version ist nicht verfügbar. Bitte andere CUDA-Version wählen.' });
        } else {
          resolve({ success: false, message: 'Update fehlgeschlagen. Überprüfen Sie die Konsole für Details.' });
        }
      });

      pip.on('error', (err) => {
        resolve({ success: false, message: 'Fehler: ' + err.message });
      });
    });
  });
});

// Update Whisper
ipcMain.handle('update-whisper', async () => {
  const pythonPath = getPythonPath();
  if (!pythonPath) {
    return { success: false, message: 'Python nicht gefunden' };
  }

  const pipPath = path.join(path.dirname(pythonPath), 'Scripts', 'pip.exe');

  return new Promise((resolve) => {
    const pip = spawn(pipPath, ['install', '--upgrade', 'openai-whisper'], { timeout: 300000 });
    let output = '';

    pip.stdout.on('data', (data) => { output += data.toString(); });
    pip.stderr.on('data', (data) => { output += data.toString(); });

    pip.on('close', (code) => {
      if (code === 0) {
        resolve({ success: true, message: 'Whisper erfolgreich aktualisiert!' });
      } else {
        resolve({ success: false, message: 'Update fehlgeschlagen.' });
      }
    });

    pip.on('error', (err) => {
      resolve({ success: false, message: 'Fehler: ' + err.message });
    });
  });
});

// Check for package updates
ipcMain.handle('check-package-updates', async () => {
  const pythonPath = getPythonPath();
  if (!pythonPath) {
    return { error: 'Python nicht gefunden' };
  }

  const pipPath = path.join(path.dirname(pythonPath), 'Scripts', 'pip.exe');

  // Helper function to get installed version
  const getInstalledVersion = (packageName) => {
    return new Promise((resolve) => {
      const pip = spawn(pipPath, ['show', packageName], { timeout: 30000 });
      let output = '';
      pip.stdout.on('data', (data) => { output += data.toString(); });
      pip.on('close', () => {
        const match = output.match(/Version:\s*(.+)/i);
        resolve(match ? match[1].trim() : null);
      });
      pip.on('error', () => resolve(null));
    });
  };

  // Helper function to get latest version from PyPI
  const getLatestVersion = (packageName) => {
    return new Promise((resolve) => {
      const pip = spawn(pipPath, ['index', 'versions', packageName], { timeout: 30000 });
      let output = '';
      pip.stdout.on('data', (data) => { output += data.toString(); });
      pip.stderr.on('data', (data) => { output += data.toString(); });
      pip.on('close', () => {
        // Output format: "package (x.y.z)" or "Available versions: x.y.z, ..."
        const versionMatch = output.match(/\(([0-9]+\.[0-9]+[0-9.]*)\)/);
        const availableMatch = output.match(/Available versions:\s*([0-9]+\.[0-9]+[0-9.]*)/);
        resolve(versionMatch ? versionMatch[1] : (availableMatch ? availableMatch[1] : null));
      });
      pip.on('error', () => resolve(null));
    });
  };

  // Helper to check if version is nightly/dev (these are always "newer")
  const isNightlyVersion = (version) => {
    if (!version) return false;
    return version.includes('dev') || version.includes('nightly') || version.includes('+');
  };

  // Helper to extract base version number for comparison
  const getBaseVersion = (version) => {
    if (!version) return [0, 0, 0];
    // Extract just the numeric parts (e.g., "2.11.0" from "2.11.0.dev20260107+cu126")
    const match = version.match(/^(\d+)\.(\d+)\.?(\d*)/);
    if (match) {
      return [parseInt(match[1]) || 0, parseInt(match[2]) || 0, parseInt(match[3]) || 0];
    }
    return [0, 0, 0];
  };

  // Compare versions: returns true if installed is older than latest
  const isUpdateAvailable = (installed, latest) => {
    if (!installed || !latest) return false;

    // Nightly/dev versions are considered up-to-date (they're bleeding edge)
    if (isNightlyVersion(installed)) return false;

    const installedParts = getBaseVersion(installed);
    const latestParts = getBaseVersion(latest);

    for (let i = 0; i < 3; i++) {
      if (latestParts[i] > installedParts[i]) return true;
      if (latestParts[i] < installedParts[i]) return false;
    }
    return false; // Same version
  };

  try {
    // Check PyTorch
    const torchInstalled = await getInstalledVersion('torch');
    const torchLatest = await getLatestVersion('torch');

    // Check Whisper
    const whisperInstalled = await getInstalledVersion('openai-whisper');
    const whisperLatest = await getLatestVersion('openai-whisper');

    const result = {
      torch: {
        installed: torchInstalled,
        latest: torchLatest,
        isNightly: isNightlyVersion(torchInstalled),
        updateAvailable: isUpdateAvailable(torchInstalled, torchLatest)
      },
      whisper: {
        installed: whisperInstalled,
        latest: whisperLatest,
        updateAvailable: isUpdateAvailable(whisperInstalled, whisperLatest)
      }
    };

    console.log('Package update check result:', result);
    return result;
  } catch (err) {
    console.error('Error checking package updates:', err);
    return { error: err.message };
  }
});

// Update all packages
ipcMain.handle('update-all', async (event, options = {}) => {
  const pythonPath = getPythonPath();
  if (!pythonPath) {
    return { success: false, message: 'Python nicht gefunden' };
  }

  const pipPath = path.join(path.dirname(pythonPath), 'Scripts', 'pip.exe');

  // Auto-detect CUDA if no indexUrl specified
  let indexUrl = options.indexUrl;
  if (indexUrl === undefined || indexUrl === '') {
    const cudaVersion = detectCudaVersion();
    indexUrl = await getPytorchCudaIndexUrlAsync(cudaVersion);
    console.log('Auto-detected CUDA for update-all, using index:', indexUrl);
  }

  return new Promise((resolve) => {
    // First update pip itself
    console.log('Updating pip...');
    const pipUpgrade = spawn(pipPath, ['install', '--upgrade', 'pip'], { timeout: 120000 });

    pipUpgrade.on('close', () => {
      // Uninstall old PyTorch first
      console.log('Uninstalling old PyTorch...');
      const uninstall = spawn(pipPath, ['uninstall', 'torch', 'torchvision', 'torchaudio', '-y'], { timeout: 60000 });

      uninstall.on('close', () => {
        // Build install arguments based on selected index URL
        let args = ['install'];

        if (indexUrl && indexUrl.includes('nightly')) {
          args.push('--pre');
        }

        args.push('torch', 'torchvision', 'torchaudio');

        if (indexUrl) {
          args.push('--index-url', indexUrl);
        }

        console.log('Installing PyTorch with args:', args.join(' '));
        const pip = spawn(pipPath, args, { timeout: 600000 });
        let output = '';

        pip.stdout.on('data', (data) => { output += data.toString(); });
        pip.stderr.on('data', (data) => { output += data.toString(); });

        pip.on('close', (code) => {
          console.log('PyTorch install exit code:', code);

          // Also update Whisper
          console.log('Updating Whisper...');
          const whisperUpdate = spawn(pipPath, ['install', '--upgrade', 'openai-whisper'], { timeout: 300000 });

          whisperUpdate.on('close', (code2) => {
            const cudaInfo = indexUrl ? indexUrl.split('/').pop() : 'CPU';
            if (code === 0 && code2 === 0) {
              resolve({ success: true, message: `Alle Pakete aktualisiert (${cudaInfo})! Neustart erforderlich.` });
            } else if (output.includes('No matching distribution')) {
              resolve({ success: false, message: 'PyTorch-Version nicht verfügbar. Bitte andere CUDA-Version wählen.' });
            } else {
              resolve({ success: false, message: 'Einige Updates sind fehlgeschlagen. Überprüfen Sie die Konsole.' });
            }
          });
        });

        pip.on('error', (err) => {
          resolve({ success: false, message: 'Fehler: ' + err.message });
        });
      });
    });
  });
});

// Window Controls
ipcMain.on('window-minimize', () => {
  mainWindow.minimize();
});

ipcMain.on('window-maximize', () => {
  if (mainWindow.isMaximized()) {
    mainWindow.unmaximize();
  } else {
    mainWindow.maximize();
  }
});

ipcMain.on('window-close', () => {
  mainWindow.hide();
});

// Floating Bar Context Menu
ipcMain.on('show-floating-menu', () => {
  const contextMenu = Menu.buildFromTemplate([
    { label: 'Whisper Voice', enabled: false },
    { type: 'separator' },
    { label: 'Hauptfenster öffnen', click: () => { mainWindow.show(); mainWindow.focus(); } },
    {
      label: 'Floating Bar ausblenden',
      click: () => {
        CONFIG.showFloatingBar = false;
        floatingBar.hide();
        updateTrayMenu();
        saveConfig();
      }
    },
    { type: 'separator' },
    { label: `Hotkey: ${CONFIG.hotkeyDisplay}`, enabled: false },
    { label: `Modell: ${CONFIG.whisperModel}`, enabled: false },
    { type: 'separator' },
    { label: 'Beenden', click: () => { app.isQuitting = true; app.quit(); } }
  ]);
  contextMenu.popup();
});

// Hotkey Recording via Helper
ipcMain.on('start-hotkey-recording', () => {
  if (helperSocket) {
    helperSocket.write('record\n');
    console.log('Hotkey-Recording gestartet');
  } else {
    console.log('Helper nicht verbunden - kann Hotkey nicht aufnehmen');
    // Notify renderer that helper isn't connected
    if (mainWindow && !mainWindow.isDestroyed()) {
      mainWindow.webContents.send('hotkey-record-failed', 'Helper nicht verbunden. Bitte App neu starten.');
    }
  }
});

ipcMain.on('cancel-hotkey-recording', () => {
  if (helperSocket) {
    helperSocket.write('cancelrecord\n');
    console.log('Hotkey-Recording abgebrochen');
  }
});

// Sprachänderung an Floating Bar weiterleiten
ipcMain.on('language-changed', (event, lang) => {
  CONFIG.appLanguage = lang;
  if (floatingBar && !floatingBar.isDestroyed()) {
    floatingBar.webContents.send('language-changed', lang);
  }
});

// ============================================
// SETUP IPC HANDLER
// ============================================

ipcMain.on('setup-language', (event, lang) => {
  CONFIG.appLanguage = lang;
});

// Detect existing Python installations with PyTorch/Whisper
ipcMain.handle('setup-detect-existing', async () => {
  console.log('Detecting existing Python installations...');
  const installations = await detectExistingInstallations();
  console.log('Found installations:', installations);
  return installations;
});

// Check if FFmpeg is available on the system
function detectFFmpeg() {
  const possiblePaths = [
    CONFIG.ffmpegPath,
    process.env.FFMPEG_PATH,
    'C:\\ffmpeg\\bin',
    'C:\\Program Files\\ffmpeg\\bin',
    path.join(INSTALL_PATH, 'ffmpeg'),
  ].filter(Boolean);

  // Check if ffmpeg is in PATH
  try {
    execSync('ffmpeg -version', { encoding: 'utf8', timeout: 5000, stdio: 'pipe' });
    return { found: true, inPath: true };
  } catch {}

  // Check common locations
  for (const ffmpegPath of possiblePaths) {
    const ffmpegExe = path.join(ffmpegPath, 'ffmpeg.exe');
    if (fs.existsSync(ffmpegExe)) {
      return { found: true, path: ffmpegPath };
    }
    if (fs.existsSync(ffmpegPath) && ffmpegPath.endsWith('ffmpeg.exe')) {
      return { found: true, path: path.dirname(ffmpegPath) };
    }
  }

  return { found: false };
}

// Detect system Python and its packages (for new setup flow)
ipcMain.handle('setup-detect-system', async () => {
  console.log('Detecting system Python...');

  // Find system Python
  const systemPythons = findSystemPythonPaths();

  if (systemPythons.length === 0) {
    return { hasPython: false };
  }

  // Check the best (newest) Python found
  const pythonPath = systemPythons[0];
  const info = await checkPythonInstallation(pythonPath);

  // Check FFmpeg
  const ffmpegInfo = detectFFmpeg();

  // Check system CUDA version vs PyTorch CUDA version
  const systemCuda = detectCudaVersion();
  let cudaUpgradeAvailable = false;
  let recommendedCudaIndex = null;

  if (systemCuda && info.hasTorch && info.cudaVersion) {
    // Parse PyTorch CUDA version (e.g., "12.8" -> {major: 12, minor: 8})
    const torchCudaMatch = info.cudaVersion.match(/(\d+)\.(\d+)/);
    if (torchCudaMatch) {
      const torchCudaMajor = parseInt(torchCudaMatch[1]);
      const torchCudaMinor = parseInt(torchCudaMatch[2]);

      // Check if system CUDA is newer than PyTorch CUDA
      if (systemCuda.major > torchCudaMajor ||
          (systemCuda.major === torchCudaMajor && systemCuda.minor > torchCudaMinor)) {
        cudaUpgradeAvailable = true;
        // Get the recommended index URL for the system CUDA version
        recommendedCudaIndex = await getPytorchCudaIndexUrlAsync(systemCuda);
        console.log(`CUDA upgrade available: PyTorch has ${info.cudaVersion}, system has ${systemCuda.major}.${systemCuda.minor}`);
      }
    }
  }

  // Determine status
  const result = {
    hasPython: true,
    pythonPath: pythonPath,
    pythonVersion: info.python ? info.python.split(' ')[0] : 'Unknown',
    hasTorch: info.hasTorch,
    torchVersion: info.torchVersion,
    cudaVersion: info.cudaVersion,
    cudaAvailable: info.cudaAvailable,
    gpuName: info.gpuName,
    gpuWorks: info.gpuWorks,
    hasWhisper: info.hasWhisper,
    hasFFmpeg: ffmpegInfo.found,
    ffmpegPath: ffmpegInfo.path || null,
    // Add system CUDA info
    systemCudaVersion: systemCuda ? `${systemCuda.major}.${systemCuda.minor}` : null,
    cudaUpgradeAvailable: cudaUpgradeAvailable,
    recommendedCudaIndex: recommendedCudaIndex,
    ready: info.hasTorch && info.hasWhisper && ffmpegInfo.found && (info.gpuWorks || !info.cudaAvailable)
  };

  console.log('System detection result:', result);
  return result;
});

// Set Python path without installing (use existing system Python)
ipcMain.handle('setup-set-python-path', async (event, pythonPath) => {
  console.log('Setting Python path to:', pythonPath);
  CONFIG.pythonPath = pythonPath;
  saveConfig();
  return { success: true };
});

// Use an existing Python installation instead of installing new
ipcMain.handle('setup-use-existing', async (event, pythonPath) => {
  console.log('Using existing Python:', pythonPath);

  // Verify the installation works
  const info = await checkPythonInstallation(pythonPath);

  if (!info.hasTorch) {
    return { success: false, message: 'PyTorch not found in this installation' };
  }

  if (!info.hasWhisper) {
    return { success: false, message: 'Whisper not found in this installation' };
  }

  // Check FFmpeg
  const ffmpegInfo = detectFFmpeg();
  if (!ffmpegInfo.found) {
    return { success: false, message: 'FFmpeg not found' };
  }

  // Save the paths to config
  CONFIG.pythonPath = pythonPath;
  CONFIG.cudaEnabled = info.gpuWorks;
  if (ffmpegInfo.path) {
    CONFIG.ffmpegPath = ffmpegInfo.path;
  }
  CONFIG.setupComplete = true;
  saveConfig();

  return {
    success: true,
    info: info,
    message: `Using ${info.torchVersion} with ${info.gpuWorks ? 'GPU' : 'CPU'} support`
  };
});

ipcMain.on('setup-install-python', async () => {
  try {
    await installPython();
  } catch (error) {
    console.error('Python installation failed:', error);
  }
});

ipcMain.on('setup-install-ffmpeg', async () => {
  try {
    await installFFmpeg();
  } catch (error) {
    console.error('FFmpeg installation failed:', error);
  }
});

ipcMain.on('setup-install-whisper', async (event, { cuda }) => {
  try {
    await installWhisper(cuda);
  } catch (error) {
    console.error('Whisper installation failed:', error);
  }
});

// Upgrade PyTorch to match system CUDA version
ipcMain.on('setup-upgrade-pytorch', async (event, { indexUrl }) => {
  const pythonPath = getPythonPath();
  if (!pythonPath) {
    if (setupWindow) {
      setupWindow.webContents.send('setup-progress', {
        step: 'pytorch-upgrade',
        progress: 0,
        status: 'Error',
        log: 'Python not found',
        type: 'error'
      });
    }
    return;
  }

  const pipPath = path.join(path.dirname(pythonPath), 'Scripts', 'pip.exe');
  const cudaLabel = indexUrl ? indexUrl.split('/').pop() : 'CPU';

  const sendProgress = (progress, status, log, type = '') => {
    if (setupWindow) {
      setupWindow.webContents.send('setup-progress', {
        step: 'pytorch-upgrade',
        progress,
        status,
        log,
        type
      });
    }
  };

  try {
    sendProgress(10, 'Uninstalling old PyTorch...', 'Removing old PyTorch installation...', 'info');

    // Uninstall old PyTorch
    await new Promise((resolve) => {
      const uninstall = spawn(pipPath, ['uninstall', 'torch', 'torchvision', 'torchaudio', '-y'], { timeout: 120000 });
      uninstall.on('close', resolve);
      uninstall.on('error', resolve);
    });

    sendProgress(30, `Installing PyTorch (${cudaLabel})...`, `Downloading PyTorch with ${cudaLabel}...`, 'info');

    // Install new PyTorch
    await runPip(pipPath, ['install', 'torch', 'torchvision', 'torchaudio', '--index-url', indexUrl], sendProgress);

    sendProgress(100, 'Complete!', `PyTorch upgraded to ${cudaLabel}!`, 'success');

    if (setupWindow) {
      setupWindow.webContents.send('setup-step-complete', 'pytorch-upgrade');
    }
  } catch (error) {
    console.error('PyTorch upgrade failed:', error);
    sendProgress(0, 'Error', `Upgrade failed: ${error.message}`, 'error');
  }
});

ipcMain.on('setup-complete', (event, { model }) => {
  completeSetup(model);
});

ipcMain.on('setup-launch-app', () => {
  if (setupWindow) {
    setupWindow.close();
  }
  launchMainApp();
});

ipcMain.on('setup-cancel', () => {
  app.quit();
});

// Haupt-App starten
async function launchMainApp() {
  createMainWindow();
  createFloatingBar();
  createTray();

  mainWindow.show();

  await startWhisperServer();

  await startHelperServer();
  startHelperProcess();

  if (!CONFIG.hotkey.win) {
    setupPushToTalk();
  }

  if (CONFIG.showFloatingBar) {
    floatingBar.show();
  }

  floatingBar.webContents.on('did-finish-load', () => {
    floatingBar.webContents.send('language-changed', CONFIG.appLanguage || 'de');
    floatingBar.webContents.send('set-hotkey', CONFIG.hotkeyDisplay);
  });

  console.log('Whisper Voice bereit!');
  console.log(`Hotkey: ${CONFIG.hotkeyDisplay}`);
}

// App Start
app.whenReady().then(async () => {
  // Konfiguration laden
  CONFIG = loadConfig();
  console.log('Konfiguration geladen:', CONFIG.hotkeyDisplay);

  // Setup-Check
  if (!CONFIG.setupComplete) {
    console.log('Setup erforderlich - starte Setup-Wizard');
    createSetupWindow();
    return;
  }

  // Normale App starten
  await launchMainApp();
});

app.on('will-quit', () => {
  uIOhook.stop();
  stopWhisperServer();
  stopHelper();
});

app.on('window-all-closed', () => {});

app.on('activate', () => {
  if (BrowserWindow.getAllWindows().length === 0) {
    createMainWindow();
  }
});
