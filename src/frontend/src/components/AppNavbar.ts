import { css, html, LitElement } from 'lit';
import { customElement } from 'lit/decorators.js';

@customElement('app-navbar')
export class AppNavbar extends LitElement {
  static styles = css`:host{display:block}`;
  protected createRenderRoot() { return this; }
  static properties = { theme: { state: true } } as any;
  declare theme: 'tov' | 'light' | string;

  connectedCallback(): void {
    super.connectedCallback();
    this.addEventListener('keydown', this.onKeydown);
    // initialize from document or storage
    const cur = document.documentElement.getAttribute('data-theme');
    this.theme = (cur as any) || 'tov';
  }

  disconnectedCallback(): void {
    this.removeEventListener('keydown', this.onKeydown);
    super.disconnectedCallback();
  }

  private onKeydown = (e: KeyboardEvent) => {
    if (e.defaultPrevented) return;
    if (e.metaKey || e.ctrlKey || e.altKey) return;
    const tag = (e.target as HTMLElement)?.tagName?.toLowerCase();
    if (tag === 'input' || tag === 'textarea') return;
    if (e.key === '/' || e.key === '\\') {
      e.preventDefault();
      this.dispatchEvent(new CustomEvent('open-search', { bubbles: true, composed: true }));
    }
  };

  private onSubmit(e: Event) {
    e.preventDefault();
    const input = this.renderRoot?.querySelector('input[name="q"]') as HTMLInputElement | null;
    const q = input?.value?.trim();
    if (!q) return;
    this.dispatchEvent(new CustomEvent('do-search', { detail: { q }, bubbles: true, composed: true }));
  }

  private openSettings = () => {
    const ev = new CustomEvent('open-settings', { bubbles: true, composed: true });
    this.dispatchEvent(ev);
  };

  private setTheme(t: 'tov' | 'light') {
    this.theme = t;
    document.documentElement.setAttribute('data-theme', t);
    try { localStorage.setItem('theme', t); } catch {}
  }

  private toggleTheme = () => {
    const next = this.theme === 'tov' ? 'light' : 'tov';
    this.setTheme(next as any);
  };

  render() {
    return html`
      <div class="navbar bg-base-100 border-b border-slate-600/60">
        <div class="flex-1">
          <label for="app-drawer" class="btn btn-ghost lg:hidden" aria-label="Open Menu">
            <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16" /></svg>
          </label>
          <a href="/" data-navigo class="btn btn-ghost text-xl">BfTools</a>
        </div>
        <div class="flex-none gap-2">
          <form class="hidden md:flex items-center" @submit=${(e: Event) => this.onSubmit(e)}>
            <label class="input input-bordered flex items-center gap-2">
              <input name="q" type="search" class="grow" placeholder="Search…" />
              <kbd class="kbd kbd-sm">↵</kbd>
            </label>
          </form>
          <button class="btn btn-ghost btn-circle" title="Toggle theme" aria-label="Toggle theme" @click=${this.toggleTheme}>
            ${this.theme === 'tov' ? html`
              <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" viewBox="0 0 24 24" fill="currentColor"><path d="M6.76 4.84l-1.8-1.79L3.17 4.84l1.79 1.79 1.8-1.79zM1 13h3v-2H1v2zm10 10h2v-3h-2v3zM4.84 19.93l1.79 1.8 1.79-1.8-1.79-1.79-1.79 1.79zM20 11V9h-3v2h3zm-2.76-6.16l1.8-1.79-1.41-1.41-1.79 1.79 1.4 1.41zM12 5a7 7 0 100 14 7 7 0 000-14z"/></svg>`
            : html`
              <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" viewBox="0 0 24 24" fill="currentColor"><path d="M21.64 13a9 9 0 11-10.63-10.63A9 9 0 0021.64 13z"/></svg>`}
          </button>
          <button class="btn btn-accent btn-circle" title="Search" @click=${() => this.dispatchEvent(new CustomEvent('open-search', { bubbles: true, composed: true }))}>
            <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" viewBox="0 0 24 24" fill="currentColor"><path d="M10 2a8 8 0 105.293 14.293l4.707 4.707 1.414-1.414-4.707-4.707A8 8 0 0010 2zm0 2a6 6 0 110 12 6 6 0 010-12z"/></svg>
          </button>
          <div class="dropdown dropdown-end">
            <div tabindex="0" role="button" class="btn btn-ghost btn-circle" aria-label="Menu">
              <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" viewBox="0 0 24 24" fill="currentColor"><path d="M12 7a2 2 0 110-4 2 2 0 010 4zm0 7a2 2 0 110-4 2 2 0 010 4zm0 7a2 2 0 110-4 2 2 0 010 4z"/></svg>
            </div>
            <ul tabindex="0" class="menu menu-sm dropdown-content bg-base-100 rounded-box z-[1] mt-3 w-52 p-2 shadow divide-y divide-slate-600/50">
              <li><a href="/settings" data-navigo @click=${this.openSettings}>Settings</a></li>
              <li><a href="/about" data-navigo>About</a></li>
              <li><a href="/sources" data-navigo>Sources</a></li>
            </ul>
          </div>
        </div>
      </div>`;
  }
}
