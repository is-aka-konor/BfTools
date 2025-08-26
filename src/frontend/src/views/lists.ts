import { html, type TemplateResult } from 'lit';
import type { Entry } from '../data/repo';
import { loadingSpinner, shell, sourceBadges } from './ui';

export function renderCategoryList(
  items: Entry[] | undefined,
  category: 'classes' | 'lineages' | 'backgrounds',
  opts: { onOpenItem?: (it: Entry) => void } = {}
): TemplateResult {
  if (!items) return loadingSpinner();
  const onOpen = opts.onOpenItem ?? (() => {});
  return html`
    <div class="mt-4 grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3">
      ${items.map(it => html`
        <a href="/${category}/${it.slug}" data-navigo class="app-card card" @click=${() => onOpen(it)}>
          <div class="card-body p-4">
            <div class="flex items-center gap-2">
              <h3 class="card-title m-0">${it.name}</h3>
              <div class="flex gap-1 flex-wrap">${sourceBadges(it.sources)}</div>
            </div>
          </div>
        </a>
      `)}
    </div>
  `;
}

export function renderCategoryDetail(
  item: Entry | undefined,
  slug: string | undefined,
  category: 'classes' | 'lineages' | 'backgrounds',
  opts: { onBackClick?: () => void } = {}
): TemplateResult {
  if (!item || item.slug !== slug) return loadingSpinner();
  const onBack = opts.onBackClick ?? (() => {});
  return html`
    <div class="mt-2">
      <a class="link" href="/${category}" data-navigo @click=${() => onBack()}>&larr; Back to ${category}</a>
      <div class="mt-2 flex items-center gap-2">
        <h2 class="text-2xl font-bold m-0">${item.name}</h2>
        <div class="flex gap-1 flex-wrap">${sourceBadges(item.sources)}</div>
      </div>
      <article class="prose max-w-none mt-4" .innerHTML=${(item as any).description ?? ''}></article>
    </div>
  `;
}

export function renderSimplePage(title: string, extra?: unknown): TemplateResult {
  return shell(title, extra);
}
