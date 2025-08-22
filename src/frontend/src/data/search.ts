import MiniSearch from 'minisearch';
import { db } from './db';

export interface SearchDoc {
  id?: string;
  name: string;
  descriptionHtml?: string;
  slug: string;
  category: string;
  sources?: Array<{ abbr: string; name: string }>;
  circle?: number;
  circleType?: string;
  school?: string;
  isRitual?: boolean;
  type?: string;
}

export async function searchAll(query: string, opts?: { fuzzy?: boolean; combineWith?: 'AND'|'OR' }): Promise<Array<{ doc: SearchDoc; score: number }>> {
  const active = (await db.meta.get('active'))?.value as Record<string, { hash: string; indexHash?: string }> | undefined;
  if (!active) return [];

  const docs: SearchDoc[] = [];
  for (const [cat, { indexHash }] of Object.entries(active)) {
    if (!indexHash) continue;
    const row = await db.indexes.get(`${cat}:${indexHash}`);
    if (!row) continue;
    const idx = row.index as { options: any; documents: SearchDoc[] };
    // Merge docs with category already embedded in each doc
    docs.push(...idx.documents);
  }

  if (docs.length === 0) return [];

  const ms = new MiniSearch<SearchDoc>({
    fields: ['name', 'descriptionHtml'],
    storeFields: ['slug', 'category', 'sources', 'circle', 'circleType', 'school', 'isRitual', 'type', 'name'],
    idField: 'id'
  });
  const prepared = docs.map(d => ({ ...d, id: `${d.category}:${d.slug}` }));
  ms.addAll(prepared);
  const fuzzyOpt: false | number = opts?.fuzzy === false ? false : 0.2;
  let results = ms.search(query, {
    boost: { name: 3, descriptionHtml: 1 },
    fuzzy: fuzzyOpt,
    combineWith: opts?.combineWith ?? 'AND',
    // When fuzzy search is on, disable prefix to avoid conflicts; otherwise enable prefix for snappy partials
    prefix: fuzzyOpt ? false : true
  });
  const mapped = results.map(r => ({ doc: r as unknown as SearchDoc, score: r.score ?? 0 }));
  // Fallback fuzzy match on names when fuzzy search is requested
  if (fuzzyOpt) {
    const q = query.trim().toLowerCase();
    const terms = q.split(/\s+/).filter(Boolean);
    if (terms.length === 1) {
      const term = terms[0];
      const dist = (a: string, b: string) => {
        const dp: number[] = Array(b.length + 1).fill(0);
        for (let j = 0; j <= b.length; j++) dp[j] = j;
        for (let i = 1; i <= a.length; i++) {
          let prev = i - 1;
          dp[0] = i;
          for (let j = 1; j <= b.length; j++) {
            const tmp = dp[j];
            dp[j] = Math.min(
              dp[j] + 1,
              dp[j - 1] + 1,
              prev + (a[i - 1] === b[j - 1] ? 0 : 1)
            );
            prev = tmp;
          }
        }
        return dp[b.length];
      };
      const hits = docs.filter(d => {
        const name = (d.name ?? '').toLowerCase();
        return name.split(/\s+/).some(w => dist(w, term) <= 1);
      });
      if (hits.length > 0) {
        const byKey = new Map<string, { doc: SearchDoc; score: number }>();
        for (const r of mapped) byKey.set(`${r.doc.category}:${r.doc.slug}`, r);
        for (const h of hits) {
          const k = `${h.category}:${h.slug}`;
          if (!byKey.has(k)) byKey.set(k, { doc: h, score: 1 });
        }
        return Array.from(byKey.values());
      }
    }
  }
  if ((opts?.combineWith ?? 'AND') === 'AND') {
    const terms = query.toLowerCase().split(/\s+/).filter(Boolean);
    const strip = (s?: string) => (s ?? '').replace(/<[^>]*>/g, ' ').toLowerCase();
    const filtered = mapped.filter(r => {
      const hay = `${(r.doc.name ?? '').toLowerCase()} ${strip(r.doc.descriptionHtml)}`;
      return terms.every(t => hay.includes(t));
    });
    if (filtered.length > 0) return filtered;
    // Fallback: if the search engine returned nothing (e.g., due to options), do a naive scan across all docs
    const all = docs
      .map(d => ({ doc: d, score: 0 }))
      .filter(r => {
        const hay = `${(r.doc.name ?? '').toLowerCase()} ${strip(r.doc.descriptionHtml)}`;
        return terms.every(t => hay.includes(t));
      });
    return all as any;
  }
  return mapped;
}
