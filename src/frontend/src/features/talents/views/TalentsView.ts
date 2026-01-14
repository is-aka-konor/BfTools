import { html, type TemplateResult } from 'lit';
import type { Talent } from '../../../data/repo';
import { loadingSpinner, sourceBadges } from '../../../core/ui/ui-utils';

export interface TalentFilters { magical: boolean; martial: boolean; src: Set<string>; q?: string; }

export interface TalentRenderOpts {
  updateTalentFilters: (patch: Partial<TalentFilters>) => void;
  rememberScroll: () => void;
}

export function renderTalents(
  items: Talent[] | undefined,
  filters: TalentFilters,
  opts: TalentRenderOpts
): TemplateResult {
  if (!items) return loadingSpinner();

  const allSources: Array<{ abbr: string; name: string }> = [];
  const seen = new Set<string>();
  for (const it of items) {
    for (const s of it.sources ?? []) {
      if (!seen.has(s.abbr)) {
        seen.add(s.abbr);
        allSources.push({ abbr: s.abbr, name: s.name });
      }
    }
  }

  const selectedSrc = filters.src;
  const q = (filters.q ?? '').trim().toLowerCase();

  let filtered = items.filter(it => {
    const raw = ((it as any).type ?? (it as any).category ?? '').toString().toLowerCase();
    const isMagical = /(mag|маг)/i.test(raw);
    const isMartial = /(mart|воин)/i.test(raw);

    const typeOk = (filters.magical && isMagical) || (filters.martial && isMartial);
    const srcOk = selectedSrc.size === 0 || (it.sources?.some(s => selectedSrc.has(s.abbr)) ?? false);
    const qOk = !q || it.name.toLowerCase().includes(q) || (it.description?.toLowerCase().includes(q) ?? false);

    return typeOk && srcOk && qOk;
  });

  return html`
    <div id="talentsPage" class="page active">
      <div class="page-header">
        <h1>Таланты</h1>
        <div class="page-controls">
          <div class="filters">
            <label class="filter-toggle">
              <span class="filter-toggle__label">Магические</span>
              <input type="checkbox" class="ui-toggle" 
                .checked=${filters.magical} 
                @change=${(e: Event) => opts.updateTalentFilters({ magical: (e.target as HTMLInputElement).checked })} />
            </label>
            <label class="filter-toggle">
              <span class="filter-toggle__label">Воинские</span>
              <input type="checkbox" class="ui-toggle" 
                .checked=${filters.martial} 
                @change=${(e: Event) => opts.updateTalentFilters({ martial: (e.target as HTMLInputElement).checked })} />
            </label>
            
            <div class="filter-chips">
              ${allSources.map(s => html`
                <button class="ui-btn ui-btn--sm ${selectedSrc.has(s.abbr) ? 'ui-btn--accent-outline' : 'ui-btn--outline'} filter-chip" 
                        title=${s.name}
                        @click=${() => {
      const next = new Set(selectedSrc);
      if (next.has(s.abbr)) next.delete(s.abbr); else next.add(s.abbr);
      opts.updateTalentFilters({ src: next });
    }}>
                  ${s.abbr}
                </button>`)}
            </div>
          </div>
          
          <input type="text" class="form-control search-input" placeholder="Поиск талантов..." 
                 .value=${filters.q ?? ''}
                 @input=${(e: Event) => opts.updateTalentFilters({ q: (e.target as HTMLInputElement).value })} />
        </div>
      </div>

      <div class="resource-grid">
        ${filtered.length === 0 ? html`
          <div class="empty-state">
            <div class="empty-state-icon">🔍</div>
            <h3>Таланты не найдены</h3>
            <p>Попробуйте изменить фильтры поиска</p>
          </div>
        ` : filtered.map(it => {
      const raw = ((it as any).type ?? (it as any).category ?? '').toString().toLowerCase();
      const isMagical = /(mag|маг)/i.test(raw);
      const isMartial = /(mart|воин)/i.test(raw);

      return html`
            <a href="/talents/${it.slug}" data-navigo class="list-card" @click=${() => opts.rememberScroll()}>
              <div class="list-card-header">
                <h3 class="list-card-name">${it.name}</h3>
                <div class="resource-badges">
                  ${isMagical ? html`<span class="ui-badge ui-badge--sm ui-badge--magical">Магия</span>` : null}
                  ${isMartial ? html`<span class="ui-badge ui-badge--sm ui-badge--martial">Воин</span>` : null}
                </div>
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

export function renderTalentDetail(
  item: Talent | undefined,
  slug: string | undefined,
  backHref: string,
  opts: { onBackClick?: () => void } = {}
): TemplateResult {
  if (!item || item.slug !== slug) return loadingSpinner();
  const onBack = opts.onBackClick ?? (() => { });

  const raw = ((item as any).type ?? (item as any).category ?? '').toString().toLowerCase();
  const isMagical = /(mag|маг)/i.test(raw);
  const isMartial = /(mart|воин)/i.test(raw);
  const typeLabel = isMagical ? 'Магический' : (isMartial ? 'Воинский' : 'Общий');

  return html`
    <div class="class-detail-page">
      <header class="class-detail-header">
        <div class="class-detail-icon">🎖️</div>
        <h1 class="class-detail-title">${item.name}</h1>
        <div class="class-detail-subtitle">Талант</div>
        <div class="detail-badges">${sourceBadges(item.sources)}</div>
      </header>

      <section class="class-meta-grid">
        <div class="meta-item">
          <div class="meta-label">Категория</div>
          <div class="meta-value">${typeLabel}</div>
        </div>
        <div class="meta-item">
          <div class="meta-label">Источник</div>
          <div class="meta-value">${item.sources?.map(s => s.abbr).join(', ') ?? '—'}</div>
        </div>
      </section>

      <section class="class-description-section">
        <h2 class="class-section-title">Описание</h2>
        <div class="prose" .innerHTML=${(item as any).description ?? ''}></div>
      </section>

      <div class="detail-actions">
        <a class="link-back" href="${backHref}" data-navigo @click=${() => onBack()}>← Вернуться к талантам</a>
      </div>
    </div>
  `;
}
