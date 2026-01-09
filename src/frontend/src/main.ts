import './style.css';
import { html, css, LitElement } from 'lit';
import { appRouter } from './core/router/AppRouter';
import { metaService } from './core/services/MetaService';
import './components/SearchModal';
import { syncContent, getCountsFromManifest } from './data/loader';
import { getDataset, getBySlug, type Entry, type Talent } from './data/repo';
import { searchAll } from './data/search';
import { renderHome } from './features/home/HomeView';
import { renderCategoryList, renderCategoryDetail, renderSimplePage } from './features/backgrounds/views/BackgroundsView';
import { renderLineages, renderLineageDetail } from './features/lineages/views/LineagesView';
import { renderClasses, renderClassDetail } from './features/classes/views/ClassesView';
import { renderSearchPage, renderResult as renderSearchResult } from './features/search/views/SearchView';
import { renderTalents, renderTalentDetail, type TalentFilters } from './features/talents/views/TalentsView';
import { renderSpells, type SpellsFilters, renderSpellDetail } from './features/spells/views/SpellsView';
import { renderLayout } from './core/ui/Layout';

export class AppRoot extends LitElement {
  protected createRenderRoot() { return this; }
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
    spellsFilters: { state: true },
    sidebarOpen: { state: true }
  } as any;

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
  declare private talentFilters: { magical: boolean; martial: boolean; src: Set<string>; q?: string };
  declare private currentItem?: Entry;
  declare private spells: Array<Entry & { circle: number; school: string; isRitual: boolean; circleType: string }> | undefined;
  declare private spellsFilters: { circle?: number | null; school?: string | null; ritual?: boolean | null; circleType?: string | null; src: Set<string>; sort: 'name-asc' | 'name-desc' | 'circle-asc' | 'circle-desc' };
  declare private sidebarOpen: boolean;

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
    this.sidebarOpen = false;
  }
  private listScroll: Record<string, number> = {};

  connectedCallback(): void {
    super.connectedCallback();
    // Apply persisted theme (if any)
    try {
      const savedTheme = localStorage.getItem('theme');
      if (savedTheme) document.documentElement.setAttribute('data-theme', savedTheme);
    } catch { }

    appRouter
      .on('/', (p) => this.setRoute('home', p), { title: 'Главная' })
      .on('/intro', (p) => this.setRoute('intro', p), { title: 'Введение' })
      .on('/spellcasting', (p) => this.setRoute('spellcasting', p), { title: 'Заклинательство' })
      .on('/classes', (p) => this.setRoute('classes', p), { title: 'Классы' })
      .on('/talents', (p) => this.setRoute('talents', p), { title: 'Таланты' })
      .on('/lineages', (p) => this.setRoute('lineages', p), { title: 'Происхождения' })
      .on('/backgrounds', (p) => this.setRoute('backgrounds', p), { title: 'Предыстории' })
      .on('/spells', (p) => this.setRoute('spells', p), { title: 'Заклинания' })
      .on('/spells/:slug', (params) => {
        this.setRoute('spell', params);
      })
      .on('/talents/:slug', (params) => this.setRoute('talent', params))
      .on('/classes/:slug', (params) => this.setRoute('class', params))
      .on('/lineages/:slug', (params) => this.setRoute('lineage', params))
      .on('/backgrounds/:slug', (params) => this.setRoute('background', params))
      .on('/search', (p) => this.setRoute('search', p), { title: 'Поиск' })
      .notFound(() => this.setRoute('notfound'))
      .resolve();
    
    appRouter.setupLinkDelegation();

    // Kick off content sync (network-first)
    this.sync();

    // Handle search events from navbar
    this.addEventListener('do-search', (e: Event) => {
      const q = (e as CustomEvent).detail?.q as string;
      if (q) appRouter.navigate(`/search?q=${encodeURIComponent(q)}`);
    });

    // Open modal from icon
    this.addEventListener('open-search', () => this.openSearchModal());

    // Keyboard shortcuts
    window.addEventListener('keydown', this.onKeydown);

    // Register service worker
    if (import.meta.env.PROD && 'serviceWorker' in navigator) {
      window.addEventListener('load', () => {
        navigator.serviceWorker.register('/sw.js', { type: 'module' });
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
    console.info('[router] setRoute', name, params);
    this.sidebarOpen = false;
    this.route = { name, params };
    if (name === 'search') this.doSearchFromLocation();
    if (name === 'classes' || name === 'lineages' || name === 'backgrounds') this.loadList(name);
    if (name === 'talents') this.loadTalents();
    if (name === 'spells') this.loadSpells();
    if (name === 'class' && params?.slug) this.loadDetail('classes', params.slug);
    if (name === 'lineage' && params?.slug) this.loadDetail('lineages', params.slug);
    if (name === 'background' && params?.slug) this.loadDetail('backgrounds', params.slug);
    if (name === 'talent' && params?.slug) this.loadTalentDetail(params.slug);
    if (name === 'spell' && params?.slug) {
      // Ensure list is loading so we can resolve detail and related items quickly
      this.loadSpells();
      this.loadSpellDetail(params.slug);
    }
  }

  render() {
    const content = this.renderRoute();
    const crumbs: Array<{ label: string; href?: string }> = [];
    const addHome = () => crumbs.push({ label: 'Главная', href: '/' });
    switch (this.route.name) {
      case 'home':
        crumbs.push({ label: 'Главная' });
        break;
      case 'spells':
        addHome();
        crumbs.push({ label: 'Заклинания' });
        break;
      case 'spell':
        addHome();
        crumbs.push({ label: 'Заклинания', href: '/spells' });
        {
          const slug = this.route.params?.slug;
          const nm = this.currentItem?.name || (this.spells?.find((x: any) => x.slug === slug) as any)?.name || slug || '...';
          crumbs.push({ label: nm });
        }
        break;
      case 'classes':
        addHome();
        crumbs.push({ label: 'Классы' });
        break;
      case 'class':
        addHome();
        crumbs.push({ label: 'Классы', href: '/classes' });
        crumbs.push({ label: this.currentItem?.name || this.route.params?.slug || '...' });
        break;
      case 'lineages':
        addHome();
        crumbs.push({ label: 'Происхождения' });
        break;
      case 'lineage':
        addHome();
        crumbs.push({ label: 'Происхождения', href: '/lineages' });
        crumbs.push({ label: this.currentItem?.name || this.route.params?.slug || '...' });
        break;
      case 'backgrounds':
        addHome();
        crumbs.push({ label: 'Предыстории' });
        break;
      case 'background':
        addHome();
        crumbs.push({ label: 'Предыстории', href: '/backgrounds' });
        crumbs.push({ label: this.currentItem?.name || this.route.params?.slug || '...' });
        break;
      case 'talents':
        addHome();
        crumbs.push({ label: 'Таланты' });
        break;
      case 'talent':
        addHome();
        crumbs.push({ label: 'Таланты', href: '/talents' });
        crumbs.push({ label: this.currentItem?.name || this.route.params?.slug || '...' });
        break;
      case 'search':
        addHome();
        crumbs.push({ label: 'Поиск' });
        break;
      default:
        addHome();
        crumbs.push({ label: '...' });
        break;
    }
    return html`
      ${renderLayout({
      routeName: this.route.name,
      content,
      counts: this.counts,
      sidebarOpen: this.sidebarOpen,
      onToggleSidebar: (val?: boolean) => (this.sidebarOpen = typeof val === 'boolean' ? val : !this.sidebarOpen),
      onSearch: (q) => { if (q) appRouter.navigate(`/search?q=${encodeURIComponent(q)}`); },
      breadcrumbs: crumbs,
      notification: this.updateReady ? html`
        <div class="alert alert-info shadow-lg flex-row gap-4">
          <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" class="stroke-current flex-shrink-0 w-6 h-6"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"></path></svg>
          <div class="flex-1">
            <span class="font-bold">Доступно обновление!</span>
            <div class="text-xs opacity-70">${this.updatedCategories.length > 0 ? `Изменения: ${this.updatedCategories.join(', ')}` : 'Новая версия приложения готова к использованию.'}</div>
          </div>
          <button class="btn btn-sm btn-ghost" @click=${() => location.reload()}>Обновить</button>
        </div>
      ` : undefined
    })}
      <search-modal .open=${this.searchOpen} @navigate=${(e: Event) => this.onNavigateFromModal(e)}></search-modal>
    `;
  }

  private renderRoute() {
    const r = this.route;
    const shell = (title: string, extra?: unknown) => renderSimplePage(title, extra as any);
    switch (r.name) {
      case 'home':
        return renderHome(this.counts);
      case 'intro': return shell('Intro');
      case 'spellcasting': return shell('Spellcasting');
      case 'classes':
        return shell('', renderClasses(this.lists['classes'] as any, { onOpenItem: () => this.rememberScroll('classes') }));
      case 'talents': return shell('', renderTalents(this.talents, this.talentFilters as TalentFilters, { updateTalentFilters: (p) => this.updateTalentFilters(p), rememberScroll: () => this.rememberScroll('talents') }));
      case 'lineages': return shell('', renderLineages(this.lists['lineages'], { onOpenItem: () => this.rememberScroll('lineages') }));
      case 'backgrounds': return shell('', renderCategoryList(this.lists['backgrounds'], 'backgrounds', { onOpenItem: () => this.rememberScroll('backgrounds') }));
      case 'spells': return renderSpells(this.spells as any, this.spellsFilters as SpellsFilters, { updateSpellsFilters: (p) => this.updateSpellsFilters(p), rememberScroll: () => this.rememberScroll('spells') });
      case 'spell': {
        const slug = this.route.params?.slug;
        const item = (this.currentItem as any) ?? (this.spells as any)?.find?.((x: any) => x.slug === slug);
        return renderSpellDetail(item, slug, { allSpells: this.spells as any });
      }
      case 'talent': {
        const q = new URLSearchParams(location.search).toString();
        const backHref = q ? `/talents?${q}` : '/talents';
        return renderTalentDetail(this.currentItem as any, this.route.params?.slug, backHref, { onBackClick: () => this.scrollBack('talents') });
      }
      case 'class': return renderClassDetail(this.currentItem as any, this.route.params?.slug, { onBackClick: () => this.scrollBack('classes') });
      case 'lineage': return renderLineageDetail(this.currentItem, this.route.params?.slug, { onBackClick: () => this.scrollBack('lineages') });
      case 'background': return renderCategoryDetail(this.currentItem, this.route.params?.slug, 'backgrounds', { onBackClick: () => this.scrollBack('backgrounds') });
      case 'search':
        return renderSearchPage(this.searchQuery, this.searching, this.searchResults, (r) => renderSearchResult(r));
      default:
        return shell('Not Found');
    }
  }

  private async loadList(category: 'classes' | 'lineages' | 'backgrounds' | 'talents' | 'spells') {
    // reuse if already loaded
    if (this.lists[category]) return;
    this.lists = { ...this.lists, [category]: await getDataset(category) };
    // after first load of list, restore scroll if any
    const y = this.listScroll[category];
    if (typeof y === 'number') queueMicrotask(() => window.scrollTo({ top: y }));
  }

  private async loadDetail(category: 'classes' | 'lineages' | 'backgrounds' | 'talents' | 'spells', slug: string) {
    this.currentItem = undefined;
    const fromList = this.lists[category]?.find(x => x.slug === slug);
    this.currentItem = fromList ?? await getBySlug(category, slug);
    
    // SEO Update for Dynamic Pages
    if (this.currentItem) {
      metaService.update({
        title: this.currentItem.name,
        description: this.currentItem.description ? this.currentItem.description.replace(/<[^>]*>/g, '').slice(0, 150) + '...' : undefined,
      });
    }
  }

  private rememberScroll(category: 'classes' | 'lineages' | 'backgrounds' | 'talents' | 'spells') {
    this.listScroll[category] = window.scrollY;
  }

  private scrollBack(category: 'classes' | 'lineages' | 'backgrounds' | 'talents' | 'spells') {
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

  private updateSpellsFilters(patch: Partial<{ circle?: number | null; school?: string | null; ritual?: boolean | null; circleType?: string | null; src?: Set<string>; sort?: 'name-asc' | 'name-desc' | 'circle-asc' | 'circle-desc' }>) {
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



  private parseTalentFiltersFromLocation() {
    const params = new URLSearchParams(location.search);
    const magical = params.get('magical');
    const martial = params.get('martial');
    const src = params.get('src');
    const q = params.get('q');
    const srcSet = new Set<string>((src ? src.split(',') : []).filter(Boolean));
    const f = {
      magical: magical ? magical === '1' : this.talentFilters.magical,
      martial: martial ? martial === '1' : this.talentFilters.martial,
      src: src ? srcSet : this.talentFilters.src,
      q: q !== null ? (q || undefined) : this.talentFilters.q
    };
    this.talentFilters = { ...f };
  }

  private updateTalentFilters(patch: Partial<{ magical: boolean; martial: boolean; src: Set<string>; q?: string }>) {
    const next = { ...this.talentFilters };
    if (patch.magical !== undefined) next.magical = patch.magical;
    if (patch.martial !== undefined) next.martial = patch.martial;
    if (patch.src) next.src = patch.src;
    if (patch.q !== undefined) next.q = patch.q;
    this.talentFilters = next;
    // update URL query params to persist state
    const sp = new URLSearchParams();
    if (next.magical) sp.set('magical', '1'); else sp.set('magical', '0');
    if (next.martial) sp.set('martial', '1'); else sp.set('martial', '0');
    if (next.src.size > 0) sp.set('src', Array.from(next.src).join(','));
    if (next.q) sp.set('q', next.q);
    history.replaceState(null, '', `/talents?${sp.toString()}`);
  }



  private async loadTalentDetail(slug: string) {
    this.currentItem = undefined;
    const fromList = this.talents?.find(x => x.slug === slug);
    this.currentItem = fromList ?? await getBySlug('talents', slug);
    
    // SEO
    if (this.currentItem) {
      metaService.update({
        title: this.currentItem.name,
        description: (this.currentItem as any).description ? (this.currentItem as any).description.replace(/<[^>]*>/g, '').slice(0, 150) + '...' : undefined,
      });
    }
  }


  private async sync() {
    try {
      const res = await syncContent();
      await this.loadCounts();
      // After content sync, refresh current route data so lists/details appear
      switch (this.route.name) {
        case 'classes':
          await this.loadList('classes');
          break;
        case 'lineages':
          await this.loadList('lineages');
          break;
        case 'backgrounds':
          await this.loadList('backgrounds');
          break;
        case 'talents':
          await this.loadTalents();
          break;
        case 'spells':
          await this.loadSpells();
          break;
        case 'class':
          if (this.route.params?.slug) await this.loadDetail('classes', this.route.params.slug);
          break;
        case 'lineage':
          if (this.route.params?.slug) await this.loadDetail('lineages', this.route.params.slug);
          break;
        case 'background':
          if (this.route.params?.slug) await this.loadDetail('backgrounds', this.route.params.slug);
          break;
        case 'talent':
          if (this.route.params?.slug) await this.loadTalentDetail(this.route.params.slug);
          break;
        case 'spell':
          // Ensure we have both list and detail when deep-linking
          await this.loadSpells();
          if (this.route.params?.slug) await this.loadSpellDetail(this.route.params.slug);
          break;
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
    if (href) appRouter.navigate(href);
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
    const fromList = this.spells?.find(x => x.slug === slug);
    if (fromList) {
      this.currentItem = fromList;
    } else {
      this.currentItem = await getBySlug('spells', slug);
    }
    
    // SEO
    if (this.currentItem) {
      metaService.update({
        title: this.currentItem.name,
        description: this.currentItem.description ? this.currentItem.description.replace(/<[^>]*>/g, '').slice(0, 150) + '...' : undefined
      });
    }
  }

}

customElements.define('app-root', AppRoot);
