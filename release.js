const fs = require('fs');
const { execSync } = require('child_process');
const path = require('path');

// Get version bump type from args (patch, minor, major) - default: patch
const bumpType = process.argv[2] || 'patch';

if (!['patch', 'minor', 'major'].includes(bumpType)) {
  console.error('Usage: node release.js [patch|minor|major]');
  console.error('  patch: 1.0.0 -> 1.0.1 (default)');
  console.error('  minor: 1.0.0 -> 1.1.0');
  console.error('  major: 1.0.0 -> 2.0.0');
  process.exit(1);
}

// Read package.json
const pkgPath = path.join(__dirname, 'package.json');
const pkg = JSON.parse(fs.readFileSync(pkgPath, 'utf8'));
const oldVersion = pkg.version;

// Bump version
const parts = oldVersion.split('.').map(Number);
if (bumpType === 'patch') parts[2]++;
if (bumpType === 'minor') { parts[1]++; parts[2] = 0; }
if (bumpType === 'major') { parts[0]++; parts[1] = 0; parts[2] = 0; }
const newVersion = parts.join('.');

// Update package.json
pkg.version = newVersion;
fs.writeFileSync(pkgPath, JSON.stringify(pkg, null, 2) + '\n');

console.log(`\n Version bumped: ${oldVersion} -> ${newVersion}\n`);

// Build installer
console.log(' Building installer...\n');
try {
  execSync('npm run build', { stdio: 'inherit', cwd: __dirname });
  console.log(`\n Done! Installer ready at: dist\\Whisper Voice Setup ${newVersion}.exe`);
  console.log(`\n Next steps:`);
  console.log(`   1. Copy files to GitHub repo`);
  console.log(`   2. Commit and push`);
  console.log(`   3. Create release v${newVersion}`);
} catch (e) {
  console.error('\n Build failed!');
  process.exit(1);
}
