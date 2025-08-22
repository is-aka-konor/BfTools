import { setupServer } from 'msw/node';
import { http, HttpResponse } from 'msw';
import fs from 'node:fs';
import path from 'node:path';

let manifestFile = 'site-manifest.json';
export function useAltManifest() { manifestFile = 'site-manifest-alt.json'; }
export function usePrimaryManifest() { manifestFile = 'site-manifest.json'; }

function readFixture(p: string) {
  const full = path.join(process.cwd(), 'tests', 'fixtures', p);
  return fs.readFileSync(full, 'utf-8');
}

export const server = setupServer(
  http.get('/__/healthy', () => HttpResponse.text('ok')),
  http.get('/dist-site/site-manifest.json', () => {
    return new HttpResponse(readFixture(manifestFile), { headers: { 'Content-Type': 'application/json' } });
  }),
  http.get('/dist-site/data/:file', ({ params }) => {
    const file = params['file'] as string;
    return new HttpResponse(readFixture(path.join('data', file)), { headers: { 'Content-Type': 'application/json' } });
  }),
  http.get('/dist-site/index/:file', ({ params }) => {
    const file = params['file'] as string;
    return new HttpResponse(readFixture(path.join('index', file)), { headers: { 'Content-Type': 'application/json' } });
  })
);
