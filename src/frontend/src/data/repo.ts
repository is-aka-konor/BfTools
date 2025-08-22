import { db } from './db';

export interface Entry {
  slug: string;
  name: string;
  descriptionHtml?: string;
  sources: Array<{ abbr: string; name: string }>
}

export interface Talent extends Entry {
  type: string;
}

export async function getDataset(category: string): Promise<Entry[]> {
  const active = (await db.meta.get('active'))?.value as Record<string, { hash: string }> | undefined;
  const h = active?.[category]?.hash;
  if (!h) return [];
  const row = await db.datasets.get(`${category}:${h}`);
  return (row?.data ?? []) as Entry[];
}

export async function getBySlug(category: string, slug: string): Promise<Entry | undefined> {
  const items = await getDataset(category);
  return items.find(x => x.slug === slug);
}
