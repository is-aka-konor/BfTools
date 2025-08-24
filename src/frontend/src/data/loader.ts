import { db } from './db';
import type { SiteManifest } from '../types/site';

export interface SyncResult {
  initial: boolean;
  changed: boolean;
  changedCategories: string[];
}

type ActiveMap = Record<string, { hash: string; indexHash?: string }>;

function toAbs(url: string): string {
  try { new URL(url); return url; } catch {}
  const origin = (globalThis as any)?.location?.origin ?? 'http://localhost';
  return origin.replace(/\/$/, '') + url;
}

async function fetchJson<T>(url: string): Promise<T> {
  const abs = toAbs(url);
  const res = await fetch(abs, { cache: 'reload' });
  if (!res.ok) throw new Error(`HTTP ${res.status} for ${url}`);
  return res.json();
}

export async function syncContent(baseUrl = ''): Promise<SyncResult> {
  const manifestUrl = `${baseUrl}/site-manifest.json`;
  const manifest = await fetchJson<SiteManifest>(manifestUrl);

  const current = (await db.meta.get('active'))?.value as ActiveMap | undefined;
  const next: ActiveMap = {};
  const changes: string[] = [];

  // Determine changes
  for (const [cat, info] of Object.entries(manifest.categories)) {
    const prev = current?.[cat];
    if (!prev || prev.hash !== info.hash || prev.indexHash !== info.indexHash) {
      changes.push(cat);
    }
    next[cat] = { hash: info.hash, indexHash: info.indexHash };
  }

  // Fetch changed datasets and indexes
  for (const cat of changes) {
    const info = manifest.categories[cat];
    // Dataset
    const dataUrl = `${baseUrl}/data/${cat}-${info.hash}.json`;
    const data = await fetchJson<any[]>(dataUrl);
    await db.datasets.put({ key: `${cat}:${info.hash}`, category: cat, hash: info.hash, data, ts: Date.now() });

    // Index
    if (info.indexHash) {
      const idxUrl = `${baseUrl}/index/${cat}-${info.indexHash}.minisearch.json`;
      const index = await fetchJson<any>(idxUrl);
      await db.indexes.put({ key: `${cat}:${info.indexHash}`, category: cat, hash: info.indexHash, index, ts: Date.now() });
    }
  }

  // Save new active map
  await db.meta.put({ key: 'active', value: next });
  await db.meta.put({ key: 'build', value: manifest.build });
  await db.meta.put({ key: 'manifest', value: manifest });

  return { initial: !current, changed: changes.length > 0 && !!current, changedCategories: changes };
}

export async function getActiveMap(): Promise<ActiveMap | undefined> {
  return (await db.meta.get('active'))?.value as ActiveMap | undefined;
}

export async function getCountsFromManifest(): Promise<Record<string, number>> {
  const manifest = (await db.meta.get('manifest'))?.value as import('../types/site').SiteManifest | undefined;
  const out: Record<string, number> = {};
  if (manifest) {
    for (const [cat, info] of Object.entries(manifest.categories)) out[cat] = info.count;
  }
  return out;
}
