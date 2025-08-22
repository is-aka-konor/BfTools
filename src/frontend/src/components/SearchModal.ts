import { css, html, LitElement, nothing } from 'lit';
import { customElement } from 'lit/decorators.js';
import { searchService } from '../data/searchService';

@customElement('search-modal')
export class SearchModal extends LitElement {
  static styles = css`:host{display:block}`;
  static properties = {
    open: { type: Boolean, reflect: true },
    q: { state: true },
    results: { state: true },
    searching: { state: true },
  } as any;
  // dynamic properties initialized in constructor
  constructor() {
    super();
    this.open = false;
    this.q = '';
    this.results = [];
    this.searching = false;
  }

  private lastFocus: HTMLElement | null = null;
  // Optional opener element provided by parent for focus restore
  public openerEl?: HTMLElement | null;

  async show() {
    this.lastFocus = (this.getRootNode() as Document | ShadowRoot).activeElement as HTMLElement | null;
    this.open = true;
    await this.updateComplete;
    const input = this.renderRoot?.querySelector('input[name="q"]') as HTMLInputElement | null;
    input?.focus();
  }

  hide() { 
    this.open = false; 
    const target = this.openerEl ?? this.lastFocus;
    queueMicrotask(() => target?.focus?.());
  }

  protected updated(changed: Map<string, unknown>) {
    if (changed.has('open')) {
      const wasOpen = changed.get('open') as boolean | undefined;
      if (this.open && !wasOpen) {
        // Property-driven open: capture focus and focus input
        this.lastFocus = (this.getRootNode() as Document | ShadowRoot).activeElement as HTMLElement | null;
        const input = this.renderRoot?.querySelector('input[name="q"]') as HTMLInputElement | null;
        input?.focus?.();
      }
    }
  }

  private async onInput(e: Event) {
    this.q = (e.target as HTMLInputElement).value;
    this.dispatchEvent(new CustomEvent('query', { detail: { q: this.q }, bubbles: true, composed: true }));
    await this.doSearch();
  }

  private async doSearch() {
    const q = this.q.trim();
    if (!q) { this.results = []; return; }
    this.searching = true;
    try {
      this.results = await searchService.search(q);
    } finally {
      this.searching = false;
    }
  }

  private grouped() {
    const groups: Record<string, Array<{ doc: any; score: number }>> = {};
    for (const r of this.results) {
      const cat = r.doc.category;
      (groups[cat] ||= []).push(r);
    }
    return groups;
  }

  private navigateTo(doc: any) {
    const href = `/${doc.category}/${doc.slug}`;
    this.dispatchEvent(new CustomEvent('navigate', { detail: { href }, bubbles: true, composed: true }));
    this.hide();
  }

  private onKeydown = (e: KeyboardEvent) => {
    if (e.key === 'Escape') { e.stopPropagation(); this.hide(); }
  };

  render() {
    return html`
      ${this.open ? html`
        <div class="modal modal-open" @keydown=${this.onKeydown}>
          <div class="modal-box max-w-3xl">
            <form method="dialog">
              <button class="btn btn-sm btn-circle btn-ghost absolute right-2 top-2" @click=${() => this.hide()}>✕</button>
            </form>
            <h3 class="font-bold text-lg mb-2">Search</h3>
            <label class="input input-bordered flex items-center gap-2">
              <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" viewBox="0 0 24 24" fill="currentColor"><path d="M10 2a8 8 0 105.293 14.293l4.707 4.707 1.414-1.414-4.707-4.707A8 8 0 0010 2zm0 2a6 6 0 110 12 6 6 0 010-12z"/></svg>
              <input name="q" type="search" class="grow" placeholder="Type to search…" .value=${this.q} @input=${(e: Event) => this.onInput(e)} />
            </label>

            ${this.searching ? html`<div class="mt-4"><span class="loading loading-spinner"></span></div>` : nothing}

            ${Object.entries(this.grouped()).length > 0 ? html`
              <div class="mt-4 space-y-4 max-h-96 overflow-auto">
                ${Object.entries(this.grouped()).map(([cat, items]) => html`
                  <div>
                    <div class="text-sm opacity-70 mb-1">${cat}</div>
                    <div class="flex flex-col gap-2">
                      ${items.map(r => html`
                        <button class="app-card card text-left" @click=${() => this.navigateTo(r.doc)}>
                          <div class="card-body p-3">
                            <div class="flex items-center gap-2">
                              <div class="font-medium">${r.doc.name}</div>
                              <div class="flex gap-1 flex-wrap">
                                ${r.doc.sources?.map((s: any) => html`<div class="tooltip" data-tip=${s.name}><span class="badge badge-outline badge-sm">${s.abbr}</span></div>`)}
                              </div>
                            </div>
                          </div>
                        </button>
                      `)}
                    </div>
                  </div>
                `)}
              </div>
            ` : (this.q ? html`<p class="opacity-70 mt-4">Nothing found.</p>` : nothing)}
          </div>
        </div>
      ` : nothing}
    `;
  }
}
