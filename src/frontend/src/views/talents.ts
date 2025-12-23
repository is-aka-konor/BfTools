import { html, type TemplateResult } from 'lit';
import type { Talent } from '../data/repo';
import { loadingSpinner, sourceBadges } from './ui';

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
            <label class="label cursor-pointer gap-2">
              <span style="font-size: var(--font-size-sm); color: var(--text-secondary);">Магические</span>
              <input type="checkbox" class="toggle toggle-sm" 
                .checked=${filters.magical} 
                @change=${(e: Event) => opts.updateTalentFilters({ magical: (e.target as HTMLInputElement).checked })} />
            </label>
            <label class="label cursor-pointer gap-2">
              <span style="font-size: var(--font-size-sm); color: var(--text-secondary);">Воинские</span>
              <input type="checkbox" class="toggle toggle-sm" 
                .checked=${filters.martial} 
                @change=${(e: Event) => opts.updateTalentFilters({ martial: (e.target as HTMLInputElement).checked })} />
            </label>
            
            <div style="display: flex; gap: 4px; align-items: center;">
              ${allSources.map(s => html`
                <button class="btn btn--sm ${selectedSrc.has(s.abbr) ? 'btn--accent-outline' : 'btn--outline'}" 
                        title=${s.name}
                        style="padding: 2px 8px; font-size: 10px; min-height: 0; height: auto;"
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
            <a href="/talents/${it.slug}" data-navigo class="resource-card" @click=${() => opts.rememberScroll()}>
              <div class="resource-header">
                <h3 class="resource-name">${it.name}</h3>
                <div style="display: flex; gap: 4px; align-items: flex-start; flex-wrap: wrap; justify-content: flex-end;">
                  ${isMagical ? html`<span class="badge--magical">Магия</span>` : null}
                  ${isMartial ? html`<span class="badge--martial">Воин</span>` : null}
                </div>
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
        <div style="margin-top: var(--space-md);">${sourceBadges(item.sources)}</div>
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

      <section class="class-description-section" style="border-left-color: var(--mystical-primary);">
        <h2 class="class-section-title">Описание</h2>
        <div class="class-full-description" .innerHTML=${(item as any).description ?? ''}></div>
      </section>

      <div style="margin-top: var(--space-xl); text-align: center;">
        <a class="btn btn--accent-outline" href="${backHref}" data-navigo @click=${() => onBack()}>← Вернуться к талантам</a>
      </div>
    </div>
  `;
}
