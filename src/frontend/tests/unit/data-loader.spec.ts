import { describe, it, expect, beforeEach, vi } from 'vitest';
import { server } from '../msw/server';
import { http, HttpResponse } from 'msw';
import { usePrimaryManifest, useAltManifest } from '../msw/server';
import { syncContent, getActiveMap } from '../../src/data/loader';
import { db } from '../../src/data/db';

function countCalls(spy: any, contains: string) {
  return spy.mock.calls.filter((args: any[]) => String(args[0]).includes(contains)).length;
}

async function resetDb() {
  await db.meta.clear();
  await db.datasets.clear();
  await db.indexes.clear();
}

describe('Data Loading & Persistence (Dexie + fake-indexeddb)', () => {
  beforeEach(async () => {
    await resetDb();
    usePrimaryManifest();
  });

  it('first-run downloads bundles and stores active hashes', async () => {
    const spy = vi.spyOn(globalThis, 'fetch');
    const res = await syncContent('/dist-site');
    expect(res.initial).toBe(true);
    expect(res.changed).toBe(false);

    // Manifest fetched once
    expect(countCalls(spy, '/dist-site/site-manifest.json')).toBe(1);
    // Two datasets and two indexes fetched
    expect(countCalls(spy, '/dist-site/data/')).toBe(2);
    expect(countCalls(spy, '/dist-site/index/')).toBe(2);

    // Dexie rows present
    const act = await getActiveMap();
    expect(act?.spells?.hash).toMatch(/^a+/);
    expect(act?.spells?.indexHash).toMatch(/^b+/);
    expect(act?.talents?.hash).toMatch(/^e+/);
    expect(act?.talents?.indexHash).toMatch(/^f+/);

    const ds = await db.datasets.toArray();
    const ix = await db.indexes.toArray();
    expect(ds.filter(r => r.category === 'spells').length).toBe(1);
    expect(ds.filter(r => r.category === 'talents').length).toBe(1);
    expect(ix.filter(r => r.category === 'spells').length).toBe(1);
    expect(ix.filter(r => r.category === 'talents').length).toBe(1);

    spy.mockRestore();
  });

  it('second-run with no changes: only manifest fetched; no bundles', async () => {
    // seed first run
    await syncContent('/dist-site');
    const spy = vi.spyOn(globalThis, 'fetch');
    const res = await syncContent('/dist-site');
    expect(res.initial).toBe(false);
    expect(res.changed).toBe(false);
    expect(countCalls(spy, '/dist-site/site-manifest.json')).toBe(1);
    expect(countCalls(spy, '/dist-site/data/')).toBe(0);
    expect(countCalls(spy, '/dist-site/index/')).toBe(0);
    spy.mockRestore();
  });

  it('update scenario: only spells changed → fetch only spells bundles and update Dexie', async () => {
    // seed first run
    await syncContent('/dist-site');
    useAltManifest();
    const spy = vi.spyOn(globalThis, 'fetch');

    const res = await syncContent('/dist-site');
    expect(res.changed).toBe(true);
    expect(res.changedCategories).toEqual(['spells']);

    // Only spells dataset+index fetched in addition to manifest
    expect(countCalls(spy, '/dist-site/site-manifest.json')).toBe(1);
    expect(countCalls(spy, '/dist-site/data/spells-')).toBe(1);
    expect(countCalls(spy, '/dist-site/index/spells-')).toBe(1);
    expect(countCalls(spy, '/dist-site/data/talents-')).toBe(0);
    expect(countCalls(spy, '/dist-site/index/talents-')).toBe(0);

    // Dexie now has 2 spell datasets (old + new) and 2 spell indexes (old + new), talents unchanged
    const ds = await db.datasets.toArray();
    const ix = await db.indexes.toArray();
    expect(ds.filter(r => r.category === 'spells').length).toBe(2);
    expect(ix.filter(r => r.category === 'spells').length).toBe(2);
    expect(ds.filter(r => r.category === 'talents').length).toBe(1);
    expect(ix.filter(r => r.category === 'talents').length).toBe(1);

    // Active map points to new spells hashes and old talents
    const act = await getActiveMap();
    expect(act?.spells?.hash).toMatch(/^c+/);
    expect(act?.spells?.indexHash).toMatch(/^d+/);
    expect(act?.talents?.hash).toMatch(/^e+/);
    expect(act?.talents?.indexHash).toMatch(/^f+/);

    spy.mockRestore();
  });

  it('error fallback: one bundle fails keeps previous state; retry works', async () => {
    // seed first run
    await syncContent('/dist-site');
    useAltManifest();

    // Force spells new dataset to fail
    server.use(
      http.get('/dist-site/data/:file', ({ params }) => {
        const file = String(params['file'] ?? '');
        if (file.startsWith('spells-cccc')) {
          return HttpResponse.text('oops', { status: 500 });
        }
        return undefined as any; // fall-through to default handler
      })
    );

    // Attempt sync: expect throw
    await expect(syncContent('/dist-site')).rejects.toBeTruthy();

    // Active map should still point to old hashes
    const act1 = await getActiveMap();
    expect(act1?.spells?.hash).toMatch(/^a+/);
    expect(act1?.spells?.indexHash).toMatch(/^b+/);

    // Remove failing override and retry
    server.resetHandlers();
    // Reapply default handlers and make sure manifest remains alt
    useAltManifest();

    const res2 = await syncContent('/dist-site');
    expect(res2.changed).toBe(true);
    expect(res2.changedCategories).toEqual(['spells']);

    const act2 = await getActiveMap();
    expect(act2?.spells?.hash).toMatch(/^c+/);
    expect(act2?.spells?.indexHash).toMatch(/^d+/);
  });
});

