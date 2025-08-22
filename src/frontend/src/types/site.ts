export interface CategoryInfo {
  hash: string;
  count: number;
  indexHash?: string;
}

export interface SiteManifest {
  build: string;
  categories: Record<string, CategoryInfo>;
  sources: Array<{ abbr: string; name: string }>;
}

