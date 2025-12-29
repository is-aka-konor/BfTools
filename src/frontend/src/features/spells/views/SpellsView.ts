import { html, type TemplateResult } from 'lit';
import type { Entry } from '../../../data/repo';
import { loadingSpinner, sourceBadges } from '../../../core/ui/ui-utils';

export type SpellsSort = 'name-asc' | 'name-desc' | 'circle-asc' | 'circle-desc';
export interface SpellsFilters { circle?: number | null; school?: string | null; ritual?: boolean | null; circleType?: string | null; src: Set<string>; sort: SpellsSort }

export interface SpellsRenderOpts {
  updateSpellsFilters: (patch: Partial<SpellsFilters>) => void;
  rememberScroll: () => void;
}

export function renderSpells(
  items: Array<Entry & { circle: number; school: string; isRitual?: boolean; circleType?: string; circles?: string[] }> | undefined,
  filters: SpellsFilters & { q?: string | null; className?: string | null },
  opts: SpellsRenderOpts
): TemplateResult {
  if (!items) return loadingSpinner();
  const f = filters;
  const allSchools = Array.from(new Set(items.map((s: any) => s.school))).sort();
  const allClasses = Array.from(new Set(items.flatMap((s: any) => (s.classes ?? s.circles ?? []) as string[]))).sort();

  let filtered = items.filter((it: any) => {
    const cOk = f.circle == null || it.circle === f.circle;
    const sOk = !f.school || it.school === f.school;
    const clOk = !f.className || (it.classes?.includes(f.className) || it.circles?.includes(f.className));
    const rOk = f.ritual == null || !!it.isRitual === f.ritual;
    const q = (f.q ?? '').trim().toLowerCase();
    const qOk = !q || it.name.toLowerCase().includes(q) || it.slug.toLowerCase().includes(q) || (it.description ? it.description.toLowerCase().includes(q) : false);
    return cOk && sOk && clOk && rOk && qOk;
  });

  const cmpName = (a: any, b: any) => a.name.localeCompare(b.name);
  const cmpCircle = (a: any, b: any) => (a.circle - b.circle) || cmpName(a, b) || a.slug.localeCompare(b.slug);
  switch (f.sort) {
    case 'name-desc': filtered = [...filtered].sort((a, b) => -cmpName(a, b)); break;
    case 'circle-asc': filtered = [...filtered].sort(cmpCircle); break;
    case 'circle-desc': filtered = [...filtered].sort((a, b) => -cmpCircle(a, b)); break;
    default: filtered = [...filtered].sort((a, b) => cmpName(a, b) || a.slug.localeCompare(b.slug));
  }

  return html`
    <div id="spellsPage" class="page active">
      <div class="page-header">
        <h1>Заклинания</h1>
        <div class="page-controls">
          <div class="filters">
            <select id="levelFilter" class="form-control" @change=${(e: Event) => opts.updateSpellsFilters({ circle: (e.target as HTMLSelectElement).value === '' ? null : Number((e.target as HTMLSelectElement).value) })}>
              <option value="" ?selected=${f.circle == null}>Все уровни</option>
              ${[0, 1, 2, 3, 4, 5, 6, 7, 8, 9].map(c => html`<option .selected=${f.circle === c} value=${c}>${c === 0 ? 'Заговоры' : `${c} уровень`}</option>`)}
            </select>
            <select id="schoolFilter" class="form-control" @change=${(e: Event) => opts.updateSpellsFilters({ school: (e.target as HTMLSelectElement).value || null })}>
              <option value="" ?selected=${!f.school}>Все школы</option>
              ${allSchools.map(s => html`<option .selected=${f.school === s} value=${s}>${s}</option>`)}
            </select>
            <select id="ritualFilter" class="form-control" @change=${(e: Event) => opts.updateSpellsFilters({ ritual: (e.target as HTMLSelectElement).value === '' ? null : (e.target as HTMLSelectElement).value === 'true' })}>
              <option value="" ?selected=${f.ritual == null}>Все (ритуалы)</option>
              <option value="true" ?selected=${f.ritual === true}>Ритуал</option>
              <option value="false" ?selected=${f.ritual === false}>Не ритуал</option>
            </select>
            <select id="classFilter" class="form-control" @change=${(e: Event) => opts.updateSpellsFilters({ className: (e.target as HTMLSelectElement).value || null } as any)}>
              <option value="" ?selected=${!(f as any).className}>Все классы</option>
              ${allClasses.map(s => html`<option .selected=${(f as any).className === s} value=${s}>${s}</option>`)}
            </select>
          </div>
          <input id="spellSearch" type="text" class="form-control search-input" placeholder="Поиск заклинаний..." 
                 .value=${f.q ?? ''}
                 @input=${(e: Event) => opts.updateSpellsFilters({ q: (e.target as HTMLInputElement).value } as any)} />
        </div>
      </div>
      <div class="spells-grid" id="spellsGrid">
        ${filtered.length === 0 ? html`
          <div class="empty-state">
            <div class="empty-state-icon">🔍</div>
            <h3>Заклинания не найдены</h3>
            <p>Попробуйте изменить фильтры поиска</p>
          </div>
        ` : filtered.map(it => html`
          <a href="/spells/${(it as any).slug}" data-navigo class="spell-card" @click=${() => opts.rememberScroll()}>
            <div class="spell-header">
              <h3 class="spell-name">${(it as any).name}</h3>
              <span class="spell-level">${(it as any).circle === 0 ? 'Заговор' : `${(it as any).circle} ур.`}</span>
            </div>
            ${((it as any).school) ? html`<div class="spell-school">${(it as any).school}${(it as any).isRitual ? ' (ритуал)' : ''}</div>` : null}
            ${((it as any).description) ? html`<div class="spell-description">${(it as any).description}</div>` : null}
            <div class="spell-tags">
              ${(it as any).isRitual ? html`<span class="spell-tag">Ритуал</span>` : null}
              ${sourceBadges((it as any).sources)}
            </div>
            ${((it as any).classes || (it as any).circles) ? html`<div class="spell-classes">Классы: ${(((it as any).classes ?? (it as any).circles) as string[]).join(', ')}</div>` : null}
          </a>
        `)}
      </div>
    </div>
  `;
}

export function renderSpellDetail(
  item: (Entry & { castingTime?: string; range?: string; components?: string; duration?: string; circle?: number; school?: string; classes?: string[]; circles?: string[]; tags?: string[]; higherLevels?: string }) | undefined,
  slug: string | undefined,
  opts?: { allSpells?: Array<Entry & { circle?: number; school?: string }> }
): TemplateResult {
  console.info('[spell-view] renderSpellDetail', { slug, hasItem: !!item });
  if (!item) return loadingSpinner();

  const level = (item as any).circle;
  const school = (item as any).school;
  const classes = ((item as any).classes ?? (item as any).circles) as string[] | undefined;
  const tags = (item as any).tags as string[] | undefined;
  const higherLevels = (item as any).higherLevels as string | undefined;

  const related = (opts?.allSpells ?? [])
    .filter((s: any) => s.slug !== item.slug && ((school && s.school === school) || (typeof level === 'number' && s.circle === level)))
    .slice(0, 3);

  return html`
    <div id="spellDetail">
      <div class="spell-detail">
        <div class="spell-detail-header">
          <h1 class="spell-detail-title">${item.name}</h1>
          <div style="color: var(--text-secondary); font-size: var(--font-size-lg);">
            ${school ? school : ''}${(school && typeof level === 'number') ? ', ' : ''}${typeof level === 'number' ? (level === 0 ? 'Заговор' : `${level} уровень`) : ''}${(item as any).isRitual ? ' (ритуал)' : ''}
          </div>
        </div>

        <div class="spell-detail-meta">
          ${(item as any).castingTime ? html`<div class="meta-item">
            <div class="meta-label">Время накладывания</div>
            <div class="meta-value">${(item as any).castingTime}</div>
          </div>` : null}
          ${(item as any).range ? html`<div class="meta-item">
            <div class="meta-label">Дистанция</div>
            <div class="meta-value">${(item as any).range}</div>
          </div>` : null}
          ${(item as any).components ? html`<div class="meta-item">
            <div class="meta-label">Компоненты</div>
            <div class="meta-value">${(item as any).components}</div>
          </div>` : null}
          ${(item as any).duration ? html`<div class="meta-item">
            <div class="meta-label">Длительность</div>
            <div class="meta-value">${(item as any).duration}</div>
          </div>` : null}
        </div>

        <div class="prose" .innerHTML=${(item as any).description ?? ''}></div>

        ${higherLevels ? html`
          <div class="spell-higher-levels">
            <h4>На более высоких уровнях</h4>
            <p>${higherLevels}</p>
          </div>
        ` : null}

        ${(classes || tags)?.length ? html`
          <div class="spell-detail-meta">
            ${classes?.length ? html`<div class="meta-item">
              <div class="meta-label">Классы</div>
              <div class="meta-value">${classes.join(', ')}</div>
            </div>` : null}
            ${tags?.length ? html`<div class="meta-item">
              <div class="meta-label">Теги</div>
              <div class="meta-value">${tags.map(t => html`<span class="spell-tag">${t}</span>`)}</div>
            </div>` : null}
          </div>
        ` : null}

        <div style="margin-top: var(--space-xl); text-align: center;">
          <a class="btn btn--accent-outline" href="/spells" data-navigo>← Вернуться к заклинаниям</a>
        </div>
      </div>

      ${related.length > 0 ? html`
        <div style="margin-top: var(--space-2xl);">
          <h3 style="margin-bottom: var(--space-lg); color: var(--accent-primary);">Похожие заклинания</h3>
          <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(250px, 1fr)); gap: var(--space-md);">
            ${related.map((s: any) => html`
              <a class="spell-card" href="/spells/${s.slug}" data-navigo>
                <div class="spell-header">
                  <h4 class="spell-name">${s.name}</h4>
                  <span class="spell-level">${typeof s.circle === 'number' ? `${s.circle} ур.` : ''}</span>
                </div>
                ${s.school ? html`<div class="spell-school">${s.school}</div>` : null}
              </a>
            `)}
          </div>
        </div>
      ` : null}
    </div>
  `;
}
