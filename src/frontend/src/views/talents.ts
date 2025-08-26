import { html, type TemplateResult } from 'lit';
import type { Talent } from '../data/repo';
import { loadingSpinner, sourceBadges } from './ui';

export interface TalentFilters { magical: boolean; martial: boolean; src: Set<string> }

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
  for (const it of items) for (const s of it.sources ?? []) if (!seen.has(s.abbr)) { seen.add(s.abbr); allSources.push({ abbr: s.abbr, name: s.name }); }

  const selectedSrc = filters.src;
  let filtered = items.filter(it => {
    const raw = ((it as any).type ?? (it as any).category ?? '').toString().toLowerCase();
    const isMagical = /(mag|маг)/i.test(raw);
    const isMartial = /(mart|воин)/i.test(raw);
    const bothOn = filters.magical && filters.martial;
    const typeOk = bothOn || (filters.magical && isMagical) || (filters.martial && isMartial);
    const srcOk = selectedSrc.size === 0 || (it.sources?.some(s => selectedSrc.has(s.abbr)) ?? false);
    return typeOk && srcOk;
  });
  if (filtered.length === 0 && items.length > 0) filtered = items;

  const toggleSrc = (abbr: string) => {
    const next = new Set(selectedSrc);
    if (next.has(abbr)) next.delete(abbr); else next.add(abbr);
    opts.updateTalentFilters({ src: next });
  };

  return html`
    <div class="mt-4 flex flex-col gap-3">
      <div class="flex flex-wrap items-center gap-4">
        <label class="label cursor-pointer gap-2">
          <span>Magical</span>
          <input type="checkbox" class="toggle" .checked=${filters.magical} @change=${(e: Event) => opts.updateTalentFilters({ magical: (e.target as HTMLInputElement).checked })} />
        </label>
        <label class="label cursor-pointer gap-2">
          <span>Martial</span>
          <input type="checkbox" class="toggle" .checked=${filters.martial} @change=${(e: Event) => opts.updateTalentFilters({ martial: (e.target as HTMLInputElement).checked })} />
        </label>
        <button class="btn btn-warning btn-sm" @click=${() => opts.updateTalentFilters({ src: new Set<string>() })}>Clear Sources</button>
      </div>
      <div class="flex flex-wrap gap-2">
        ${allSources.map(s => html`
          <div class="tooltip" data-tip=${s.name}>
            <button class="btn btn-xs ${selectedSrc.has(s.abbr) ? 'btn-accent' : 'btn-ghost'}" 
                    data-active=${selectedSrc.has(s.abbr) ? '1' : '0'}
                    @click=${() => toggleSrc(s.abbr)}>
              <span class="badge ${selectedSrc.has(s.abbr) ? 'badge-primary' : 'badge-outline'}">${s.abbr}</span>
            </button>
          </div>`)}
      </div>
      <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3">
        ${filtered.map(it => html`
          <a href="/talents/${it.slug}" data-navigo class="app-card card" @click=${() => opts.rememberScroll()}>
            <div class="card-body p-4">
              <div class="flex items-center gap-2">
                <h3 class="card-title m-0">${it.name}</h3>
                <div class="flex gap-1 flex-wrap">${sourceBadges(it.sources)}</div>
              </div>
            </div>
          </a>
        `)}
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
  const onBack = opts.onBackClick ?? (() => {});
  return html`
    <div class="mt-2">
      <a class="link" href="${backHref}" data-navigo @click=${() => onBack()}>&larr; Back to talents</a>
      <div class="mt-2 flex items-center gap-2">
        <h2 class="text-2xl font-bold m-0">${item.name}</h2>
        <div class="flex gap-1 flex-wrap">${sourceBadges(item.sources)}</div>
      </div>
      <article class="prose max-w-none mt-4" .innerHTML=${(item as any).description ?? ''}></article>
    </div>
  `;
}

