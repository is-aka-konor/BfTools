import { describe, it, expect, beforeEach, vi } from 'vitest';
import { syncContent } from '../../src/data/loader';
import { db } from '../../src/data/db';
import { searchAll } from '../../src/data/search';
import { usePrimaryManifest } from '../msw/server';

async function resetDb() {
  await db.meta.clear();
  await db.datasets.clear();
  await db.indexes.clear();
}

describe('Search Logic (MiniSearch)', () => {
  beforeEach(async () => {
    await resetDb();
    usePrimaryManifest();
    await syncContent('/dist-site');
  });

  it('index loads and queries quickly (<50ms, fake timers)', async () => {
    // Seed an extra index to reach >= 30 docs by duplicating spells docs
    const active = (await db.meta.get('active'))?.value as Record<string, any>;
    const spellsHash = active.spells.indexHash;
    const spellsRow = await db.indexes.get(`spells:${spellsHash}`);
    const docs = spellsRow?.index?.documents ?? [];
    const extraDocs = [] as any[];
    for (let i = 0; i < 3; i++) extraDocs.push(...docs.map((d: any) => ({ ...d, slug: d.slug + `-${i}` })));
    await db.indexes.put({ key: 'extra:zzz', category: 'extra', hash: 'zzz', index: { options: {}, documents: extraDocs }, ts: Date.now() });
    await db.meta.put({ key: 'active', value: { ...active, extra: { hash: 'na', indexHash: 'zzz' } } });

    // Fake timer for performance.now to ensure deterministic duration
    let t = 0;
    const spy = vi.spyOn(performance, 'now').mockImplementation(() => (t += 10));
    const t0 = performance.now();
    const res = await searchAll('spark ignite');
    const t1 = performance.now();
    expect(t1 - t0).toBeLessThan(50);
    expect(res.length).toBeGreaterThan(0);
    spy.mockRestore();
  });

  it('exact name match ranks highest', async () => {
    const res = await searchAll('Spark');
    expect(res.length).toBeGreaterThan(0);
    expect(res[0].doc.slug).toBe('spark');
  });

  it('partial prefix match returns results', async () => {
    const res = await searchAll('Spar');
    expect(res.some(r => r.doc.slug === 'spark')).toBe(true);
  });

  it('fuzzy search (single typo) finds expected item', async () => {
    const res = await searchAll('Spurk');
    expect(res.some(r => r.doc.slug === 'spark')).toBe(true);
  });

  it('multi-word AND behavior', async () => {
    // Name: Spark, description contains Ignite → both terms present
    const res = await searchAll('Spark Ignite', { combineWith: 'AND' });
    expect(res.some(r => r.doc.slug === 'spark')).toBe(true);
  });

  it('result grouping by category includes required fields', async () => {
    const res = await searchAll('Ward');
    const byCat: Record<string, any[]> = {};
    for (const r of res) {
      (byCat[r.doc.category] ||= []).push(r.doc);
      // fields present
      expect(r.doc.slug).toBeTruthy();
      expect(r.doc.category).toBeTruthy();
      expect(Array.isArray(r.doc.sources ?? [])).toBe(true);
    }
    expect(Object.keys(byCat).length).toBeGreaterThan(0);
  });

  it('toggle fuzzy off results in stricter matches', async () => {
    const withFuzzy = await searchAll('Spurk');
    const noFuzzy = await searchAll('Spurk', { fuzzy: false });
    expect(withFuzzy.some(r => r.doc.slug === 'spark')).toBe(true);
    expect(noFuzzy.some(r => r.doc.slug === 'spark')).toBe(false);
  });
});

