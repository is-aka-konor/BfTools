import { setupServer } from 'msw/node';
import { http, HttpResponse } from 'msw';
import fs from 'node:fs';
import path from 'node:path';

const rootDist = path.join('..', '..', 'dist-site');
let manifestFile = path.join(rootDist, 'site-manifest.json');
export function useAltManifest() { manifestFile = path.join('tests','fixtures','site-manifest-alt.json'); }
export function usePrimaryManifest() { manifestFile = path.join(rootDist, 'site-manifest.json'); }

function readFileFromRoot(p: string) {
  const full = path.isAbsolute(p) ? p : path.join(process.cwd(), p);
  return fs.readFileSync(full, 'utf-8');
}

export const server = setupServer(
  http.get('/__/healthy', () => HttpResponse.text('ok')),
  http.get('/site-manifest.json', () => {
    return new HttpResponse(readFileFromRoot(manifestFile), { headers: { 'Content-Type': 'application/json' } });
  }),
  http.get('/data/:file', ({ params }) => {
    const file = params['file'] as string;
    const direct = path.join(rootDist,'data', file);
    if (fs.existsSync(path.isAbsolute(direct) ? direct : path.join(process.cwd(), direct))) {
      return new HttpResponse(readFileFromRoot(direct), { headers: { 'Content-Type': 'application/json' } });
    }
    // Fallback: serve the first available file for this category from dist-site/data
    const category = String(file).split('-')[0];
    const dataDir = path.join(rootDist, 'data');
    const absDataDir = path.isAbsolute(dataDir) ? dataDir : path.join(process.cwd(), dataDir);
    try {
      const files = fs.readdirSync(absDataDir).filter(f => f.startsWith(`${category}-`) && f.endsWith('.json'));
      if (files.length === 0) return new HttpResponse('Not found', { status: 404 });
      const chosen = path.join(absDataDir, files[0]);
      const body = fs.readFileSync(chosen, 'utf-8');
      return new HttpResponse(body, { headers: { 'Content-Type': 'application/json' } });
    } catch {
      return new HttpResponse('Not found', { status: 404 });
    }
  }),
  http.get('/index/:file', ({ params }) => {
    const file = params['file'] as string;
    const direct = path.join(rootDist,'index', file);
    if (fs.existsSync(path.isAbsolute(direct) ? direct : path.join(process.cwd(), direct))) {
      return new HttpResponse(readFileFromRoot(direct), { headers: { 'Content-Type': 'application/json' } });
    }
    // Fallback: serve the first available index for this category
    const category = String(file).split('-')[0];
    const idxDir = path.join(rootDist, 'index');
    const absIdxDir = path.isAbsolute(idxDir) ? idxDir : path.join(process.cwd(), idxDir);
    try {
      const files = fs.readdirSync(absIdxDir).filter(f => f.startsWith(`${category}-`) && f.endsWith('.json'));
      if (files.length === 0) return new HttpResponse('Not found', { status: 404 });
      const chosen = path.join(absIdxDir, files[0]);
      const body = fs.readFileSync(chosen, 'utf-8');
      return new HttpResponse(body, { headers: { 'Content-Type': 'application/json' } });
    } catch {
      return new HttpResponse('Not found', { status: 404 });
    }
  })
);
