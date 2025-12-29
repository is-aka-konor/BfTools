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
            <a href="/lineages/${it.slug}" data-navigo class="resource-card" @click=${() => onOpen(it)}>
              <div class="resource-header">
                <h3 class="resource-name">${it.name}</h3>
              </div>
              
              <div class="resource-meta">
                ${it.size ? html`<div><strong>Размер:</strong> ${it.size}</div>` : ''}
                ${it.speed ? html`<div><strong>Скорость:</strong> ${it.speed} ft</div>` : ''}
                ${ageTrait ? html`
                  <div class="mt-1">
                    <strong>Возраст:</strong> 
                    <span style="display: -webkit-box; -webkit-line-clamp: 2; line-clamp: 2; -webkit-box-orient: vertical; overflow: hidden; opacity: 0.8;">
                      ${ageTrait.description}
                    </span>
                  </div>
                ` : ''}
              </div>

              <div style="margin-top: auto; display: flex; justify-content: flex-end;">
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
        <div style="margin-top: var(--space-md);">${sourceBadges(item.sources)}</div>
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

      <section class="class-description-section" style="border-left-color: var(--mystical-primary);">
        <h2 class="class-section-title">Описание</h2>
        <div class="class-full-description" .innerHTML=${item.description ?? ''}></div>
      </section>

      <div style="margin-top: var(--space-xl); text-align: center;">
        <a class="btn btn--accent-outline" href="/lineages" data-navigo @click=${() => onBack()}>← Вернуться к происхождениям</a>
      </div>
    </div>
  `;
}
