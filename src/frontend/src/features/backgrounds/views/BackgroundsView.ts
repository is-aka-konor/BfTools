import { html, type TemplateResult } from 'lit';
import type { Entry } from '../../../data/repo';
import { loadingSpinner, shell, sourceBadges } from '../../../core/ui/ui-utils';

export function renderCategoryList(
  items: Entry[] | undefined,
  category: 'classes' | 'lineages' | 'backgrounds',
  opts: { onOpenItem?: (it: Entry) => void } = {}
): TemplateResult {
  if (!items) return loadingSpinner();
  const onOpen = opts.onOpenItem ?? (() => { });
  const displayTitles: Record<string, string> = {
    lineages: 'Происхождения',
    backgrounds: 'Предыстории',
    classes: 'Классы'
  };

  return html`
    <div id="${category}Page" class="page active">
      <div class="page-header">
        <h1>${displayTitles[category] || category}</h1>
      </div>
      <div class="resource-grid">
        ${items.map(it => html`
          <a href="/${category}/${it.slug}" data-navigo class="list-card" @click=${() => onOpen(it)}>
            <div class="list-card-header">
              <h3 class="list-card-name">${it.name}</h3>
            </div>
            <div class="resource-footer">
              ${sourceBadges(it.sources)}
            </div>
          </a>
        `)}
      </div>
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
  const onBack = opts.onBackClick ?? (() => { });

  const categoryLabels: Record<string, string> = {
    backgrounds: 'Предыстория',
    classes: 'Класс'
  };
  const categoryTitle = categoryLabels[category] || category;

  const backLabels: Record<string, string> = {
    backgrounds: 'к предысториям',
    classes: 'к классам'
  };
  const backTo = backLabels[category] || category;

  return html`
    <div class="class-detail-page">
      <header class="class-detail-header">
        <div class="class-detail-icon">📜</div>
        <h1 class="class-detail-title">${item.name}</h1>
        <div class="class-detail-subtitle">${categoryTitle}</div>
        <div class="detail-badges">${sourceBadges(item.sources)}</div>
      </header>

      <section class="class-meta-grid">
        <div class="meta-item">
          <div class="meta-label">Категория</div>
          <div class="meta-value">${categoryTitle}</div>
        </div>
        <div class="meta-item">
          <div class="meta-label">Источник</div>
          <div class="meta-value">${item.sources?.map(s => s.abbr).join(', ') ?? '—'}</div>
        </div>
      </section>

      <section class="class-description-section">
        <h2 class="class-section-title">Описание</h2>
        <div class="prose" .innerHTML=${item.description ?? ''}></div>
      </section>

      <div class="detail-actions">
        <a class="link-back" href="/${category}" data-navigo @click=${() => onBack()}>← Вернуться ${backTo}</a>
      </div>
    </div>
  `;
}

export function renderSimplePage(title: string, extra?: unknown): TemplateResult {
  return shell(title, extra);
}
