import { css, html, LitElement, nothing } from 'lit';
import { customElement } from 'lit/decorators.js';
import { searchService } from '../data/searchService';

@customElement('search-modal')
export class SearchModal extends LitElement {
  static styles = css`:host{display:block}`;
  protected createRenderRoot() { return this; }
  static properties = {
    open: { type: Boolean, reflect: true },
    q: { state: true },
    results: { state: true },
    searching: { state: true },
  } as any;

  declare open: boolean;
  declare q: string;
  declare results: Array<{ doc: any; score: number }>;
  declare searching: boolean;
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
        <div class="ui-modal" @keydown=${this.onKeydown}>
          <div class="ui-modal__dialog" role="dialog" aria-modal="true">
            <div class="ui-modal__header">
              <h3 class="ui-modal__title">Search</h3>
              <button class="ui-btn ui-btn--ghost ui-btn--icon" @click=${() => this.hide()} aria-label="Close search">✕</button>
            </div>
            <div class="ui-modal__body">
              <label class="ui-field">
                <svg xmlns="http://www.w3.org/2000/svg" class="ui-field__icon" viewBox="0 0 24 24" fill="currentColor" aria-hidden="true"><path d="M10 2a8 8 0 105.293 14.293l4.707 4.707 1.414-1.414-4.707-4.707A8 8 0 0010 2zm0 2a6 6 0 110 12 6 6 0 010-12z"/></svg>
                <input name="q" type="search" class="ui-field__input" placeholder="Type to search…" .value=${this.q} @input=${(e: Event) => this.onInput(e)} />
              </label>

              ${this.searching ? html`<div class="ui-modal__status"><span class="ui-spinner"></span></div>` : nothing}

              ${Object.entries(this.grouped()).length > 0 ? html`
                <div class="result-groups">
                  ${Object.entries(this.grouped()).map(([cat, items]) => html`
                    <div class="result-group">
                      <div class="result-group__title">${cat}</div>
                      <div class="result-list result-list--compact">
                        ${items.map(r => html`
                          <button class="ui-card ui-card--interactive result-item" @click=${() => this.navigateTo(r.doc)}>
                            <div class="ui-card__body">
                              <div class="result-item__header">
                                <div class="result-item__name">${r.doc.name}</div>
                                <div class="result-item__badges">
                                  ${r.doc.sources?.map((s: any) => html`
                                    <div class="ui-tooltip" data-tip=${s.name}>
                                      <span class="ui-badge ui-badge--outline ui-badge--sm ui-badge--source">${s.abbr}</span>
                                    </div>
                                  `)}
                                </div>
                              </div>
                            </div>
                          </button>
                        `)}
                      </div>
                    </div>
                  `)}
                </div>
              ` : (this.q ? html`<p class="text-muted">Nothing found.</p>` : nothing)}
            </div>
          </div>
        </div>
      ` : nothing}
    `;
  }
}
