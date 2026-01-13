import MiniSearch from 'minisearch';
import { db } from './db';

export type SearchDoc = {
  name: string;
  description?: string;
  slug: string;
  category: string;
  sources?: Array<{ abbr: string; name: string }>;
  circle?: number;
  circleType?: string;
  school?: string;
  isRitual?: boolean;
  type?: string;
};

class SearchService {
  private ms?: MiniSearch<SearchDoc>;
  private ready = false;

  async ensureReady(): Promise<void> {
    if (this.ready && this.ms) return;
    const active = (await db.meta.get('active'))?.value as Record<string, { indexHash?: string }> | undefined;
    if (!active) { this.ms = new MiniSearch({ fields: ['name', 'description'] }); this.ready = true; return; }

    const docs: SearchDoc[] = [];
    for (const [cat, { indexHash }] of Object.entries(active)) {
      if (!indexHash) continue;
      const row = await db.indexes.get(cat);
      if (!row || row.hash !== indexHash) continue;
      const idx = row.index as { documents: SearchDoc[] };
      docs.push(...idx.documents);
    }

    const ms = new MiniSearch<SearchDoc>({
      fields: ['name', 'description'],
      storeFields: ['slug', 'category', 'sources', 'circle', 'circleType', 'school', 'isRitual', 'type', 'name'],
      idField: 'id'
    });
    const prepared = docs.map(d => ({ ...d, id: `${d.category}:${d.slug}` }));
    ms.addAll(prepared);
    this.ms = ms;
    this.ready = true;
  }

  async search(query: string): Promise<Array<{ doc: SearchDoc; score: number }>> {
    await this.ensureReady();
    if (!this.ms || !query.trim()) return [];
    const results = this.ms.search(query, { boost: { name: 3, description: 1 }, fuzzy: 0.2, prefix: true, combineWith: 'AND' });
    return results.map(r => ({ doc: r as unknown as SearchDoc, score: r.score ?? 0 }));
  }
}

export const searchService = new SearchService();
