import { html, type TemplateResult } from 'lit';
import { loadingSpinner, sourceBadges } from '../../../core/ui/ui-utils';

export function renderLineages(
  items: any[] | undefined,
  opts: { onOpenItem?: (it: any) => void } = {}
): TemplateResult {
  if (!items) return loadingSpinner();
  const onOpen = opts.onOpenItem ?? (() => { });

  return html`
    <div id="lineagesPage" class="page active">
      <div class="page-header">
        <h1>Происхождения</h1>
      </div>
      <div class="resource-grid">
        ${items.map(it => {
    const ageTrait = it.traits?.find((t: any) => t.name === 'Возраст');

    return html`
            <a href="/lineages/${it.slug}" data-navigo class="list-card" @click=${() => onOpen(it)}>
              <div class="list-card-header">
                <h3 class="list-card-name">${it.name}</h3>
              </div>
              
              <div class="resource-meta">
                ${it.size ? html`<div><strong>Размер:</strong> ${it.size}</div>` : ''}
                ${it.speed ? html`<div><strong>Скорость:</strong> ${it.speed} ft</div>` : ''}
                ${ageTrait ? html`
                  <div class="resource-meta__item">
                    <strong>Возраст:</strong> 
                    <span class="line-clamp-2 text-subtle">
                      ${ageTrait.description}
                    </span>
                  </div>
                ` : ''}
              </div>

              <div class="resource-footer">
                ${sourceBadges(it.sources)}
              </div>
            </a>
          `;
  })}
      </div>
    </div>
  `;
}

export function renderLineageDetail(
  item: any | undefined,
  slug: string | undefined,
  opts: { onBackClick?: () => void } = {}
): TemplateResult {
  if (!item || item.slug !== slug) return loadingSpinner();
  const onBack = opts.onBackClick ?? (() => { });

  return html`
    <div class="class-detail-page">
      <header class="class-detail-header">
        <div class="class-detail-icon">🛡️</div>
        <h1 class="class-detail-title">${item.name}</h1>
        <div class="class-detail-subtitle">Происхождение</div>
        <div class="detail-badges">${sourceBadges(item.sources)}</div>
      </header>

      <section class="class-meta-grid">
        <div class="meta-item">
          <div class="meta-label">Размер</div>
          <div class="meta-value">${item.size || '—'}</div>
        </div>
        <div class="meta-item">
          <div class="meta-label">Скорость</div>
          <div class="meta-value">${item.speed ? `${item.speed} ft` : '—'}</div>
        </div>
        <div class="meta-item">
          <div class="meta-label">Источник</div>
          <div class="meta-value">${item.sources?.map((s: any) => s.abbr).join(', ') ?? '—'}</div>
        </div>
      </section>

      <section class="class-description-section">
        <h2 class="class-section-title">Описание</h2>
        <div class="prose" .innerHTML=${item.description ?? ''}></div>
      </section>

      <div class="detail-actions">
        <a class="ui-btn ui-btn--accent-outline" href="/lineages" data-navigo @click=${() => onBack()}>← Вернуться к происхождениям</a>
      </div>
    </div>
  `;
}
