import './style.css';
import { html, css, LitElement } from 'lit';
import Navigo from 'navigo';
import './components/AppNavbar';
import './components/AppDrawer';
import './components/SearchModal';
import { syncContent, getCountsFromManifest } from './data/loader';
import { getDataset, getBySlug, type Entry, type Talent } from './data/repo';
import { searchAll } from './data/search';

export class AppRoot extends LitElement {
  static styles = css`
    :host { display: block; }
  `;
  static properties = {
    route: { state: true },
    updateReady: { state: true },
    updatedCategories: { state: true },
    searchQuery: { state: true },
    searchResults: { state: true },
    searching: { state: true },
    counts: { state: true },
    searchOpen: { state: true },
    lists: { state: true },
    talents: { state: true },
    talentFilters: { state: true },
    currentItem: { state: true },
    spells: { state: true },
    spellsFilters: { state: true }
  } as any;

  private router = new Navigo('/');
  declare private route: { name: string; params?: Record<string, string> };
  declare private updateReady: boolean;
  declare private updatedCategories: string[];
  declare private searchQuery: string;
  declare private searchResults: Array<{ doc: any; score: number }>;
  declare private searching: boolean;
  declare private counts: Record<string, number>;
  declare private searchOpen: boolean;
  declare private lists: Record<string, Entry[]>;
  declare private talents: Talent[] | undefined;
  declare private talentFilters: { magical: boolean; martial: boolean; src: Set<string> };
  declare private currentItem?: Entry;
  declare private spells: Array<Entry & { circle:number; school:string; isRitual:boolean; circleType:string }> | undefined;
  declare private spellsFilters: { circle?: number|null; school?: string|null; ritual?: boolean|null; circleType?: string|null; src: Set<string>; sort: 'name-asc'|'name-desc'|'circle-asc'|'circle-desc' };

  constructor() {
    super();
    this.route = { name: 'home' };
    this.updateReady = false;
    this.updatedCategories = [];
    this.searchQuery = '';
    this.searchResults = [];
    this.searching = false;
    this.counts = {};
    this.searchOpen = false;
    this.lists = {};
    this.talents = undefined;
    this.talentFilters = { magical: true, martial: true, src: new Set() };
    this.currentItem = undefined;
    this.spells = undefined;
    this.spellsFilters = { circle: null, school: null, ritual: null, circleType: null, src: new Set(), sort: 'name-asc' };
  }
  private listScroll: Record<string, number> = {};

  connectedCallback(): void {
    super.connectedCallback();
    this.router
      .on('/', () => this.setRoute('home'))
      .on('/intro', () => this.setRoute('intro'))
      .on('/spellcasting', () => this.setRoute('spellcasting'))
      .on('/classes', () => this.setRoute('classes'))
      .on('/talents', () => this.setRoute('talents'))
      .on('/lineages', () => this.setRoute('lineages'))
      .on('/backgrounds', () => this.setRoute('backgrounds'))
      .on('/spells', () => this.setRoute('spells'))
      .on('/spells/:slug', ({ data }) => this.setRoute('spell', data))
      .on('/talents/:slug', ({ data }) => this.setRoute('talent', data))
      .on('/classes/:slug', ({ data }) => this.setRoute('class', data))
      .on('/lineages/:slug', ({ data }) => this.setRoute('lineage', data))
      .on('/backgrounds/:slug', ({ data }) => this.setRoute('background', data))
      .on('/search', () => this.setRoute('search'))
      .notFound(() => this.setRoute('notfound'))
      .resolve();

    // Kick off content sync (network-first)
    this.sync();

    // Handle search events from navbar
    this.addEventListener('do-search', (e: Event) => {
      const q = (e as CustomEvent).detail?.q as string;
      if (q) this.router.navigate(`/search?q=${encodeURIComponent(q)}`);
    });

    // Open modal from icon
    this.addEventListener('open-search', () => this.openSearchModal());

    // Keyboard shortcuts
    window.addEventListener('keydown', this.onKeydown);

    // Register service worker
    if ('serviceWorker' in navigator) {
      window.addEventListener('load', () => {
        navigator.serviceWorker.register('/sw.js');
      });
      navigator.serviceWorker.addEventListener('message', (event) => {
        if ((event.data as any)?.type === 'sw-updated') {
          // trigger update snackbar flow
          this.updateReady = true;
        }
      });
    }
  }

  private setRoute(name: string, params?: Record<string, string>) {
    this.route = { name, params };
    if (name === 'search') this.doSearchFromLocation();
    if (name === 'classes' || name === 'lineages' || name === 'backgrounds') this.loadList(name);
    if (name === 'talents') this.loadTalents();
    if (name === 'spells') this.loadSpells();
    if (name === 'class' && params?.slug) this.loadDetail('classes', params.slug);
    if (name === 'lineage' && params?.slug) this.loadDetail('lineages', params.slug);
    if (name === 'background' && params?.slug) this.loadDetail('backgrounds', params.slug);
    if (name === 'talent' && params?.slug) this.loadTalentDetail(params.slug);
    if (name === 'spell' && params?.slug) this.loadSpellDetail(params.slug);
  }

  render() {
    return html`
      <app-drawer>
        <app-navbar></app-navbar>
        <main class="p-4 lg:p-6">
          ${this.renderRoute()}
        </main>
      </app-drawer>
      <search-modal .open=${this.searchOpen} @navigate=${(e: Event) => this.onNavigateFromModal(e)}></search-modal>
      ${this.updateReady ? html`<div class="toast toast-bottom toast-center">
        <div class="alert alert-info">
          <span>${this.updatedCategories.length > 0 ? `New content available (${this.updatedCategories.join(', ')})` : 'New version available'}.</span>
          <button class="btn btn-sm ml-2" @click=${() => location.reload()}>Reload</button>
        </div>
      </div>` : null}
    `;
  }

  private renderRoute() {
    const r = this.route;
    const shell = (title: string, extra?: unknown) => html`
      <div class="prose max-w-none">
        <h1>${title}</h1>
        ${extra ?? ''}
      </div>`;

    switch (r.name) {
      case 'home':
        return html`
          ${shell('Home')}
          <div class="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-6 gap-4 mt-4">
            ${this.tile('/intro', 'Intro / Rules')}
            ${this.tile('/spellcasting', 'Spellcasting')}
            ${this.tile('/classes', 'Classes', this.counts['classes'])}
            ${this.tile('/talents', 'Talents', this.counts['talents'])}
            ${this.tile('/lineages', 'Lineages', this.counts['lineages'])}
            ${this.tile('/spells', 'Spells', this.counts['spells'])}
            ${this.tile('/backgrounds', 'Backgrounds', this.counts['backgrounds'])}
          </div>`;
      case 'intro': return shell('Intro');
      case 'spellcasting': return shell('Spellcasting');
      case 'classes': return shell('Classes', this.renderList('classes'));
      case 'talents': return shell('Talents', this.renderTalents());
      case 'lineages': return shell('Lineages', this.renderList('lineages'));
      case 'backgrounds': return shell('Backgrounds', this.renderList('backgrounds'));
      case 'spells':
        return shell('Spells', this.renderSpells());
      case 'spell': return this.renderSpellDetail();
      case 'talent': return this.renderTalentDetail();
      case 'class': return this.renderDetail('classes');
      case 'lineage': return this.renderDetail('lineages');
      case 'background': return this.renderDetail('backgrounds');
      case 'search':
        return html`
          ${shell('Search', html`<p class="mt-2">Results for <code>${this.searchQuery || '(empty)'}</code></p>`)}
          ${this.searching ? html`<span class="loading loading-spinner"></span>` : this.renderSearchResults()}
        `;
      default:
        return shell('Not Found');
    }
  }

  private tile(href: string, label: string, count?: number) {
    return html`
      <a href="${href}" data-navigo class="app-card card hover:shadow-lg transition-shadow">
        <div class="card-body p-4">
          <div class="flex items-center justify-between">
            <h3 class="card-title">${label}</h3>
            ${typeof count === 'number' ? html`<span class="badge badge-sm">${count}</span>` : null}
          </div>
        </div>
      </a>`;
  }

  private renderList(category: 'classes'|'lineages'|'backgrounds') {
    const items = this.lists[category];
    if (!items) return html`<span class="loading loading-spinner"></span>`;
    return html`
      <div class="mt-4 grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3">
        ${items.map(it => html`
          <a href="/${category}/${it.slug}" data-navigo class="app-card card" @click=${() => this.rememberScroll(category)}>
            <div class="card-body p-4">
              <div class="flex items-center gap-2">
                <h3 class="card-title m-0">${it.name}</h3>
                <div class="flex gap-1 flex-wrap">
                  ${it.sources?.map(s => html`<div class="tooltip" data-tip=${s.name}><span class="badge badge-outline badge-sm">${s.abbr}</span></div>`)}
                </div>
              </div>
            </div>
          </a>
        `)}
      </div>`;
  }

  private renderDetail(category: 'classes'|'lineages'|'backgrounds') {
    const item = this.currentItem;
    const slug = this.route.params?.slug;
    if (!item || item.slug !== slug) return html`<span class="loading loading-spinner"></span>`;
    return html`
      <div class="mt-2">
        <a class="link" href="/${category}" data-navigo @click=${() => this.scrollBack(category)}>&larr; Back to ${category}</a>
        <div class="mt-2 flex items-center gap-2">
          <h2 class="text-2xl font-bold m-0">${item.name}</h2>
          <div class="flex gap-1 flex-wrap">
            ${item.sources?.map(s => html`<div class="tooltip" data-tip=${s.name}><span class="badge badge-outline badge-sm">${s.abbr}</span></div>`)}
          </div>
        </div>
        <article class="prose max-w-none mt-4" .innerHTML=${item.descriptionHtml ?? ''}></article>
      </div>`;
  }

  private async loadList(category: 'classes'|'lineages'|'backgrounds') {
    // reuse if already loaded
    if (this.lists[category]) return;
    this.lists = { ...this.lists, [category]: await getDataset(category) };
    // after first load of list, restore scroll if any
    const y = this.listScroll[category];
    if (typeof y === 'number') queueMicrotask(() => window.scrollTo({ top: y }));
  }

  private async loadDetail(category: 'classes'|'lineages'|'backgrounds', slug: string) {
    this.currentItem = undefined;
    const fromList = this.lists[category]?.find(x => x.slug === slug);
    this.currentItem = fromList ?? await getBySlug(category, slug);
  }

  private rememberScroll(category: 'classes'|'lineages'|'backgrounds') {
    this.listScroll[category] = window.scrollY;
  }

  private scrollBack(category: 'classes'|'lineages'|'backgrounds') {
    const y = this.listScroll[category] ?? 0;
    queueMicrotask(() => window.scrollTo({ top: y }));
  }

  private async doSearchFromLocation() {
    const params = new URLSearchParams(location.search);
    const q = params.get('q') ?? '';
    this.searchQuery = q;
    this.searchResults = [];
    if (!q) return;
    this.searching = true;
    try {
      this.searchResults = await searchAll(q);
    } finally {
      this.searching = false;
    }
  }

  private renderSearchResults() {
    if (!this.searchQuery) return html`<p class="opacity-70">Type in the search bar to find content.</p>`;
    if (this.searchResults.length === 0) return html`<p class="opacity-70">Nothing was found for “${this.searchQuery}”.</p>`;
    return html`
      <div class="mt-4 space-y-3">
        ${this.searchResults.map(r => this.renderResult(r))}
      </div>`;
  }

  private async loadCounts() {
    this.counts = await getCountsFromManifest();
  }

  private async loadTalents() {
    // Parse filters from URL on first enter
    this.parseTalentFiltersFromLocation();
    if (!this.talents) {
      this.talents = await getDataset('talents') as unknown as Talent[];
      // Restore scroll
      const y = this.listScroll['talents'];
      if (typeof y === 'number') queueMicrotask(() => window.scrollTo({ top: y }));
    }
  }

  private async loadSpells() {
    this.parseSpellsFiltersFromLocation();
    if (!this.spells) {
      const data = await getDataset('spells');
      this.spells = data as any;
      const y = this.listScroll['spells'];
      if (typeof y === 'number') queueMicrotask(() => window.scrollTo({ top: y }));
    }
  }

  private parseSpellsFiltersFromLocation() {
    const p = new URLSearchParams(location.search);
    const circle = p.get('circle');
    const school = p.get('school');
    const ritual = p.get('ritual');
    const ct = p.get('circleType');
    const src = p.get('src');
    const sort = (p.get('sort') as any) || this.spellsFilters.sort;
    this.spellsFilters = {
      circle: circle !== null ? (circle === '' ? null : Number(circle)) : this.spellsFilters.circle,
      school: school !== null ? (school || null) : this.spellsFilters.school,
      ritual: ritual !== null ? (ritual === '' ? null : ritual === '1') : this.spellsFilters.ritual,
      circleType: ct !== null ? (ct || null) : this.spellsFilters.circleType,
      src: src ? new Set(src.split(',').filter(Boolean)) : this.spellsFilters.src,
      sort: sort ?? 'name-asc'
    } as any;
  }

  private updateSpellsFilters(patch: Partial<{ circle?: number|null; school?: string|null; ritual?: boolean|null; circleType?: string|null; src?: Set<string>; sort?: 'name-asc'|'name-desc'|'circle-asc'|'circle-desc' }>) {
    const next = { ...this.spellsFilters } as any;
    for (const [k, v] of Object.entries(patch)) (next as any)[k] = v;
    this.spellsFilters = next;
    const sp = new URLSearchParams();
    if (next.circle !== null && next.circle !== undefined) sp.set('circle', String(next.circle));
    if (next.school) sp.set('school', next.school);
    if (next.ritual !== null && next.ritual !== undefined) sp.set('ritual', next.ritual ? '1' : '0');
    if (next.circleType) sp.set('circleType', next.circleType);
    if (next.src?.size > 0) sp.set('src', Array.from(next.src).join(','));
    if (next.sort) sp.set('sort', next.sort);
    history.replaceState(null, '', `/spells?${sp.toString()}`);
  }

  private renderSpells() {
    const items = this.spells;
    if (!items) return html`<span class="loading loading-spinner"></span>`;
    const f = this.spellsFilters;
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
      this.updateSpellsFilters({ src: next });
    };

    const clearAll = () => this.updateSpellsFilters({ circle: null, school: null, ritual: null, circleType: null, src: new Set(), sort: 'name-asc' });

    return html`
      <div class="mt-4 flex flex-col gap-3">
        <div class="flex flex-wrap items-center gap-3">
          <label class="form-control w-40">
            <div class="label"><span class="label-text">Circle</span></div>
            <select class="select select-bordered"
              @change=${(e:Event)=> this.updateSpellsFilters({ circle: (e.target as HTMLSelectElement).value === '' ? null : Number((e.target as HTMLSelectElement).value) })}>
              <option value="" ?selected=${f.circle==null}>All</option>
              ${[0,1,2,3,4,5,6,7,8,9].map(c => html`<option .selected=${f.circle===c} value=${c}>${c}</option>`)}
            </select>
          </label>
          <label class="form-control w-52">
            <div class="label"><span class="label-text">School</span></div>
            <select class="select select-bordered"
              @change=${(e:Event)=> this.updateSpellsFilters({ school: (e.target as HTMLSelectElement).value || null })}>
              <option value="" ?selected=${!f.school}>All</option>
              ${allSchools.map(s => html`<option .selected=${f.school===s} value=${s}>${s}</option>`)}
            </select>
          </label>
          <label class="label cursor-pointer gap-2">
            <span>Ritual</span>
            <input type="checkbox" class="toggle" .checked=${f.ritual===true}
              indeterminate=${String(f.ritual==null)}
              @change=${(e:Event)=> this.updateSpellsFilters({ ritual: (e.target as HTMLInputElement).checked ? true : null })} />
          </label>
          <label class="form-control w-44">
            <div class="label"><span class="label-text">Circle Type</span></div>
            <select class="select select-bordered"
              @change=${(e:Event)=> this.updateSpellsFilters({ circleType: (e.target as HTMLSelectElement).value || null })}>
              <option value="" ?selected=${!f.circleType}>All</option>
              ${Array.from(new Set(items.map((x:any)=>x.circleType))).sort().map(ct => html`<option .selected=${f.circleType===ct} value=${ct}>${ct}</option>`)}
            </select>
          </label>
          <label class="form-control w-52">
            <div class="label"><span class="label-text">Sort</span></div>
            <select class="select select-bordered" @change=${(e:Event)=> this.updateSpellsFilters({ sort: (e.target as HTMLSelectElement).value as any })}>
              ${['name-asc','name-desc','circle-asc','circle-desc'].map(k => html`<option .selected=${f.sort===k} value=${k}>${k}</option>`)}
            </select>
          </label>
          <button class="btn btn-sm" @click=${clearAll}>Clear All</button>
        </div>
        <div class="flex flex-wrap gap-2">
          ${allSources.map(s => html`
            <div class="tooltip" data-tip=${s.name}>
              <button class="btn btn-xs" data-active=${selectedSrc.has(s.abbr)?'1':'0'} @click=${() => toggleSrc(s.abbr)}>
                <span class="badge ${selectedSrc.has(s.abbr)?'badge-primary':'badge-outline'}">${s.abbr}</span>
              </button>
            </div>`)}
        </div>
        <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3">
          ${filtered.map(it => html`
            <a href="/spells/${(it as any).slug}" data-navigo class="app-card card" @click=${() => this.rememberScroll('spells')}>
              <div class="card-body p-4">
                <div class="flex items-center gap-2">
                  <h3 class="card-title m-0">${(it as any).name}</h3>
                  <span class="badge badge-sm">${(it as any).circle}</span>
                  <div class="flex gap-1 flex-wrap">
                    ${it.sources?.map(s => html`<div class="tooltip" data-tip=${s.name}><span class="badge badge-outline badge-sm">${s.abbr}</span></div>`)}
                  </div>
                </div>
              </div>
            </a>
          `)}
        </div>
      </div>
    `;
  }

  private parseTalentFiltersFromLocation() {
    const params = new URLSearchParams(location.search);
    const magical = params.get('magical');
    const martial = params.get('martial');
    const src = params.get('src');
    const srcSet = new Set<string>((src ? src.split(',') : []).filter(Boolean));
    const f = {
      magical: magical ? magical === '1' : this.talentFilters.magical,
      martial: martial ? martial === '1' : this.talentFilters.martial,
      src: src ? srcSet : this.talentFilters.src
    };
    this.talentFilters = { ...f };
  }

  private updateTalentFilters(patch: Partial<{ magical: boolean; martial: boolean; src: Set<string> }>) {
    const next = { ...this.talentFilters };
    if (patch.magical !== undefined) next.magical = patch.magical;
    if (patch.martial !== undefined) next.martial = patch.martial;
    if (patch.src) next.src = patch.src;
    this.talentFilters = next;
    // update URL query params to persist state
    const sp = new URLSearchParams();
    if (next.magical) sp.set('magical', '1'); else sp.set('magical', '0');
    if (next.martial) sp.set('martial', '1'); else sp.set('martial', '0');
    if (next.src.size > 0) sp.set('src', Array.from(next.src).join(','));
    history.replaceState(null, '', `/talents?${sp.toString()}`);
  }

  private renderTalents() {
    const items = this.talents;
    if (!items) return html`<span class="loading loading-spinner"></span>`;
    const allSources: Array<{ abbr: string; name: string }> = [];
    const seen = new Set<string>();
    for (const it of items) {
      for (const s of it.sources ?? []) {
        if (!seen.has(s.abbr)) { seen.add(s.abbr); allSources.push({ abbr: s.abbr, name: s.name }); }
      }
    }

    const selectedSrc = this.talentFilters.src;
    const filtered = items.filter(it => {
      const typeOk = (this.talentFilters.magical && /magical/i.test((it as any).type)) || (this.talentFilters.martial && /martial/i.test((it as any).type));
      const srcOk = selectedSrc.size === 0 || (it.sources?.some(s => selectedSrc.has(s.abbr)) ?? false);
      return typeOk && srcOk;
    });

    const toggleSrc = (abbr: string) => {
      const next = new Set(selectedSrc);
      if (next.has(abbr)) next.delete(abbr); else next.add(abbr);
      this.updateTalentFilters({ src: next });
    };

    return html`
      <div class="mt-4 flex flex-col gap-3">
        <div class="flex flex-wrap items-center gap-4">
          <label class="label cursor-pointer gap-2">
            <span>Magical</span>
            <input type="checkbox" class="toggle" .checked=${this.talentFilters.magical} @change=${(e: Event) => this.updateTalentFilters({ magical: (e.target as HTMLInputElement).checked })} />
          </label>
          <label class="label cursor-pointer gap-2">
            <span>Martial</span>
            <input type="checkbox" class="toggle" .checked=${this.talentFilters.martial} @change=${(e: Event) => this.updateTalentFilters({ martial: (e.target as HTMLInputElement).checked })} />
          </label>
          <button class="btn btn-sm" @click=${() => this.updateTalentFilters({ src: new Set<string>() })}>Clear Sources</button>
        </div>
        <div class="flex flex-wrap gap-2">
          ${allSources.map(s => html`
            <div class="tooltip" data-tip=${s.name}>
              <button class="btn btn-xs" 
                      data-active=${selectedSrc.has(s.abbr) ? '1' : '0'}
                      @click=${() => toggleSrc(s.abbr)}>
                <span class="badge ${selectedSrc.has(s.abbr) ? 'badge-primary' : 'badge-outline'}">${s.abbr}</span>
              </button>
            </div>`)}
        </div>
        <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3">
          ${filtered.map(it => html`
            <a href="/talents/${it.slug}" data-navigo class="app-card card" @click=${() => this.rememberScroll('talents')}>
              <div class="card-body p-4">
                <div class="flex items-center gap-2">
                  <h3 class="card-title m-0">${it.name}</h3>
                  <div class="flex gap-1 flex-wrap">
                    ${it.sources?.map(s => html`<div class="tooltip" data-tip=${s.name}><span class="badge badge-outline badge-sm">${s.abbr}</span></div>`)}
                  </div>
                </div>
              </div>
            </a>
          `)}
        </div>
      </div>
    `;
  }

  private async loadTalentDetail(slug: string) {
    this.currentItem = undefined;
    const fromList = this.talents?.find(x => x.slug === slug);
    this.currentItem = fromList ?? await getBySlug('talents', slug);
  }

  private renderTalentDetail() {
    const item = this.currentItem;
    const slug = this.route.params?.slug;
    if (!item || item.slug !== slug) return html`<span class="loading loading-spinner"></span>`;
    const q = new URLSearchParams(location.search).toString();
    const backHref = q ? `/talents?${q}` : '/talents';
    return html`
      <div class="mt-2">
        <a class="link" href="${backHref}" data-navigo @click=${() => this.scrollBack('talents')}>&larr; Back to talents</a>
        <div class="mt-2 flex items-center gap-2">
          <h2 class="text-2xl font-bold m-0">${item.name}</h2>
          <div class="flex gap-1 flex-wrap">
            ${item.sources?.map(s => html`<div class="tooltip" data-tip=${s.name}><span class="badge badge-outline badge-sm">${s.abbr}</span></div>`)}
          </div>
        </div>
        <article class="prose max-w-none mt-4" .innerHTML=${item.descriptionHtml ?? ''}></article>
      </div>`;
  }

  private renderResult(r: { doc: any; score: number }) {
    const d = r.doc;
    const href = `/${d.category}/${d.slug}`;
    return html`
      <a href="${href}" data-navigo class="app-card block">
        <div class="card-body p-4">
          <div class="flex items-center gap-2">
            <h3 class="card-title m-0">${d.name}</h3>
            <span class="app-badge badge-sm">${d.category}</span>
          </div>
          ${d.descriptionHtml ? html`<p class="line-clamp-2 opacity-80" .innerHTML=${d.descriptionHtml}></p>` : ''}
        </div>
      </a>`;
  }

  private async sync() {
    try {
      const res = await syncContent('/dist-site');
      await this.loadCounts();
      // If we are on a detail route that relies on datasets, retry loading now that sync completed
      if (this.route.name === 'spell' && this.route.params?.slug && !this.currentItem) {
        await this.loadSpellDetail(this.route.params.slug);
      }
      if (res.changed) {
        this.updatedCategories = res.changedCategories;
        this.updateReady = true;
      }
    } catch (err) {
      // swallow for now; could surface as a toast error later
      console.error('sync failed', err);
    }
  }

  private openSearchModal() {
    const modal = this.renderRoot?.querySelector('search-modal') as any;
    if (modal) modal.openerEl = (this.getRootNode() as Document | ShadowRoot).activeElement as HTMLElement | null;
    this.searchOpen = true;
  }

  private onNavigateFromModal(e: Event) {
    const href = (e as CustomEvent).detail?.href as string;
    if (href) this.router.navigate(href);
    this.searchOpen = false;
  }

  private onKeydown = (e: KeyboardEvent) => {
    if (e.defaultPrevented) return;
    if (e.metaKey || e.ctrlKey || e.altKey) return;
    const tag = (e.target as HTMLElement)?.tagName?.toLowerCase();
    if (tag === 'input' || tag === 'textarea') return;
    if (e.key === '/' || e.key === '\\') {
      e.preventDefault();
      this.openSearchModal();
    }
  };

  private async loadSpellDetail(slug: string) {
    this.currentItem = undefined;
    this.currentItem = await getBySlug('spells', slug);
  }

  private renderSpellDetail() {
    const item = this.currentItem;
    const slug = this.route.params?.slug;
    if (!item || item.slug !== slug) return html`<span class="loading loading-spinner"></span>`;
    return html`
      <div class="mt-2">
        <a class="link" href="/spells" data-navigo>&larr; Back to spells</a>
        <div class="mt-2 flex items-center gap-2">
          <h2 class="text-2xl font-bold m-0">${item.name}</h2>
          <div class="flex gap-1 flex-wrap">
            ${item.sources?.map(s => html`<div class="tooltip" data-tip=${s.name}><span class="badge badge-outline badge-sm">${s.abbr}</span></div>`)}
          </div>
        </div>
        <article class="prose max-w-none mt-4" .innerHTML=${item.descriptionHtml ?? ''}></article>
      </div>`;
  }

}

customElements.define('app-root', AppRoot);
