import { html, type TemplateResult } from 'lit';
import type { Entry } from '../data/repo';
import { loadingSpinner, sourceBadges } from './ui';

export type SpellsSort = 'name-asc'|'name-desc'|'circle-asc'|'circle-desc';
export interface SpellsFilters { circle?: number|null; school?: string|null; ritual?: boolean|null; circleType?: string|null; src: Set<string>; sort: SpellsSort }

export interface SpellsRenderOpts {
  updateSpellsFilters: (patch: Partial<SpellsFilters>) => void;
  rememberScroll: () => void;
}

export function renderSpells(
  items: Array<Entry & { circle:number; school:string; isRitual:boolean; circleType:string }> | undefined,
  filters: SpellsFilters,
  opts: SpellsRenderOpts
): TemplateResult {
  if (!items) return loadingSpinner();
  const f = filters;
  const allSchools = Array.from(new Set(items.map((s: any) => s.school))).sort();
  const allSources: Array<{ abbr: string; name: string }> = [];
  const seen = new Set<string>();
  for (const it of items) for (const s of it.sources ?? []) if (!seen.has(s.abbr)) { seen.add(s.abbr); allSources.push({ abbr: s.abbr, name: s.name }); }
  const selectedSrc = f.src;

  let filtered = items.filter((it: any) => {
    const cOk = f.circle == null || it.circle === f.circle;
    const sOk = !f.school || it.school === f.school;
    const rOk = f.ritual == null || it.isRitual === f.ritual;
    const ctOk = !f.circleType || it.circleType === f.circleType;
    const srcOk = selectedSrc.size === 0 || (it.sources?.some((s: any) => selectedSrc.has(s.abbr)) ?? false);
    return cOk && sOk && rOk && ctOk && srcOk;
  });

  const cmpName = (a:any,b:any) => a.name.localeCompare(b.name);
  const cmpCircle = (a:any,b:any) => (a.circle - b.circle) || cmpName(a,b) || a.slug.localeCompare(b.slug);
  switch (f.sort) {
    case 'name-desc': filtered = [...filtered].sort((a,b)=>-cmpName(a,b)); break;
    case 'circle-asc': filtered = [...filtered].sort(cmpCircle); break;
    case 'circle-desc': filtered = [...filtered].sort((a,b)=>-cmpCircle(a,b)); break;
    default: filtered = [...filtered].sort((a,b)=>cmpName(a,b) || a.slug.localeCompare(b.slug));
  }

  const toggleSrc = (abbr: string) => {
    const next = new Set(selectedSrc);
    if (next.has(abbr)) next.delete(abbr); else next.add(abbr);
    opts.updateSpellsFilters({ src: next });
  };

  const clearAll = () => opts.updateSpellsFilters({ circle: null, school: null, ritual: null, circleType: null, src: new Set(), sort: 'name-asc' });

  return html`
    <div class="mt-4 flex flex-col gap-3">
      <div class="flex flex-wrap items-center gap-3">
        <label class="form-control w-40">
          <div class="label"><span class="label-text">Circle</span></div>
          <select class="select select-bordered"
            @change=${(e:Event)=> opts.updateSpellsFilters({ circle: (e.target as HTMLSelectElement).value === '' ? null : Number((e.target as HTMLSelectElement).value) })}>
            <option value="" ?selected=${f.circle==null}>All</option>
            ${[0,1,2,3,4,5,6,7,8,9].map(c => html`<option .selected=${f.circle===c} value=${c}>${c}</option>`)}
          </select>
        </label>
        <label class="form-control w-52">
          <div class="label"><span class="label-text">School</span></div>
          <select class="select select-bordered"
            @change=${(e:Event)=> opts.updateSpellsFilters({ school: (e.target as HTMLSelectElement).value || null })}>
            <option value="" ?selected=${!f.school}>All</option>
            ${allSchools.map(s => html`<option .selected=${f.school===s} value=${s}>${s}</option>`)}
          </select>
        </label>
        <label class="label cursor-pointer gap-2">
          <span>Ritual</span>
          <input type="checkbox" class="toggle" .checked=${f.ritual===true}
            indeterminate=${String(f.ritual==null)}
            @change=${(e:Event)=> opts.updateSpellsFilters({ ritual: (e.target as HTMLInputElement).checked ? true : null })} />
        </label>
        <label class="form-control w-44">
          <div class="label"><span class="label-text">Circle Type</span></div>
          <select class="select select-bordered"
            @change=${(e:Event)=> opts.updateSpellsFilters({ circleType: (e.target as HTMLSelectElement).value || null })}>
            <option value="" ?selected=${!f.circleType}>All</option>
            ${Array.from(new Set(items.map((x:any)=>x.circleType))).sort().map(ct => html`<option .selected=${f.circleType===ct} value=${ct}>${ct}</option>`)}
          </select>
        </label>
        <label class="form-control w-52">
          <div class="label"><span class="label-text">Sort</span></div>
          <select class="select select-bordered" @change=${(e:Event)=> opts.updateSpellsFilters({ sort: (e.target as HTMLSelectElement).value as any })}>
            ${['name-asc','name-desc','circle-asc','circle-desc'].map(k => html`<option .selected=${f.sort===k} value=${k}>${k}</option>`)}
          </select>
        </label>
        <button class="btn btn-warning btn-sm" @click=${clearAll}>Clear All</button>
      </div>
      <div class="flex flex-wrap gap-2">
        ${allSources.map(s => html`
          <div class="tooltip" data-tip=${s.name}>
            <button class="btn btn-xs ${selectedSrc.has(s.abbr)?'btn-accent':'btn-ghost'}" data-active=${selectedSrc.has(s.abbr)?'1':'0'} @click=${() => toggleSrc(s.abbr)}>
              <span class="badge ${selectedSrc.has(s.abbr)?'badge-primary':'badge-outline'}">${s.abbr}</span>
            </button>
          </div>`)}
      </div>
      <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3">
        ${filtered.map(it => html`
          <a href="/spells/${(it as any).slug}" data-navigo class="app-card card" @click=${() => opts.rememberScroll()}>
            <div class="card-body p-4">
              <div class="flex items-center gap-2">
                <h3 class="card-title m-0">${(it as any).name}</h3>
                <span class="badge badge-sm">${(it as any).circle}</span>
                <div class="flex gap-1 flex-wrap">${sourceBadges((it as any).sources)}</div>
              </div>
            </div>
          </a>
        `)}
      </div>
    </div>
  `;
}

export function renderSpellDetail(
  item: Entry | undefined,
  slug: string | undefined
): TemplateResult {
  if (!item || item.slug !== slug) return loadingSpinner();
  return html`
    <div class="mt-2">
      <a class="link" href="/spells" data-navigo>&larr; Back to spells</a>
      <div class="mt-2 flex items-center gap-2">
        <h2 class="text-2xl font-bold m-0">${item.name}</h2>
        <div class="flex gap-1 flex-wrap">${sourceBadges(item.sources)}</div>
      </div>
      <article class="prose max-w-none mt-4" .innerHTML=${(item as any).description ?? ''}></article>
    </div>
  `;
}

