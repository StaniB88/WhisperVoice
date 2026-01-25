/**
 * Post-build script to set the application icon on the Windows executable.
 *
 * This is necessary because signAndEditExecutable: false prevents electron-builder
 * from embedding the icon, but that setting is required on systems without
 * Windows Developer Mode enabled (due to symlink permission issues).
 *
 * Usage: node scripts/set-icon.js
 */

const { rcedit } = require('rcedit');
const path = require('path');

const exePath = path.join(__dirname, '..', 'dist', 'win-unpacked', 'Whisper Voice.exe');
const iconPath = path.join(__dirname, '..', 'build', 'icon.ico');

console.log('Setting icon and version info on executable...');
console.log('  EXE:', exePath);
console.log('  Icon:', iconPath);

rcedit(exePath, {
  icon: iconPath,
  'version-string': {
    'ProductName': 'Whisper Voice',
    'FileDescription': 'Whisper Voice - Speech to Text',
    'CompanyName': 'AnyAutomation',
    'LegalCopyright': 'Copyright © 2024 AnyAutomation',
    'OriginalFilename': 'Whisper Voice.exe',
    'InternalName': 'Whisper Voice'
  },
  'product-version': '1.0.0',
  'file-version': '1.0.0'
})
  .then(() => {
    console.log('Icon and version info set successfully!');
  })
  .catch((err) => {
    console.error('Failed to set icon:', err.message);
    process.exit(1);
  });
