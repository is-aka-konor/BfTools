#!/usr/bin/env node
const semverGte = (a, b) => {
  const pa = a.split('.').map(Number);
  const pb = b.split('.').map(Number);
  for (let i = 0; i < 3; i++) {
    if ((pa[i] || 0) > (pb[i] || 0)) return true;
    if ((pa[i] || 0) < (pb[i] || 0)) return false;
  }
  return true;
};

const min = '18.19.0';
const cur = process.versions.node;
if (!semverGte(cur, min)) {
  console.log(`[playwright] Skipped: Node ${cur} < required ${min}`);
  process.exit(0);
}

const { spawn } = require('node:child_process');
const child = spawn(process.platform === 'win32' ? 'npx.cmd' : 'npx', ['playwright', 'test'], { stdio: 'inherit' });
child.on('exit', (code) => process.exit(code));

