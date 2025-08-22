#!/usr/bin/env node
import { spawn } from 'node:child_process';

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

const run = (cmd, args = []) => new Promise((resolve, reject) => {
  const child = spawn(process.platform === 'win32' ? `${cmd}.cmd` : cmd, args, { stdio: 'inherit', shell: false });
  child.on('exit', code => code === 0 ? resolve(0) : reject(new Error(`${cmd} ${args.join(' ')} exited ${code}`)));
});

try {
  await run('npx', ['playwright', 'test']);
} catch (err) {
  console.error('[playwright] Failed:', err?.message || err);
  process.exit(1);
}
