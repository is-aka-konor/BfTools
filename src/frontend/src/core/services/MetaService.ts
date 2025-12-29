export interface RouteMeta {
  title: string;
  description?: string;
  keywords?: string[];
  image?: string;
  type?: 'website' | 'article' | 'book';
}

export class MetaService {
  private defaultTitle = 'Tales of the Valiant';
  private defaultDesc = 'Полный справочник по правилам игры Tales of the Valiant (Black Flag Roleplaying).';

  constructor() {}

  update(meta: RouteMeta) {
    document.title = meta.title 
      ? `${meta.title} | ${this.defaultTitle}` 
      : this.defaultTitle;

    this.setMeta('description', meta.description || this.defaultDesc);
    this.setMeta('keywords', (meta.keywords || []).join(', '));
    
    // OG
    this.setMeta('og:title', meta.title || this.defaultTitle);
    this.setMeta('og:description', meta.description || this.defaultDesc);
    this.setMeta('og:type', meta.type || 'website');
    if (meta.image) this.setMeta('og:image', meta.image);

    // Twitter
    this.setMeta('twitter:card', 'summary_large_image');
    this.setMeta('twitter:title', meta.title || this.defaultTitle);
    this.setMeta('twitter:description', meta.description || this.defaultDesc);
    if (meta.image) this.setMeta('twitter:image', meta.image);
  }

  private setMeta(name: string, content: string) {
    let tag = document.querySelector(`meta[name="${name}"]`) || document.querySelector(`meta[property="${name}"]`);
    if (!tag) {
      tag = document.createElement('meta');
      if (name.startsWith('og:') || name.startsWith('twitter:')) {
        tag.setAttribute('property', name);
      } else {
        tag.setAttribute('name', name);
      }
      document.head.appendChild(tag);
    }
    tag.setAttribute('content', content);
  }
}

export const metaService = new MetaService();
