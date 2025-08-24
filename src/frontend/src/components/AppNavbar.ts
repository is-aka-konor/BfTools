import { css, html, LitElement } from 'lit';
import { customElement } from 'lit/decorators.js';

@customElement('app-navbar')
export class AppNavbar extends LitElement {
  static styles = css`:host{display:block}`;
  protected createRenderRoot() { return this; }

  connectedCallback(): void {
    super.connectedCallback();
    this.addEventListener('keydown', this.onKeydown);
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

  render() {
    return html`
      <div class="navbar bg-base-100 border-b border-base-200">
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
          <button class="btn btn-ghost btn-circle" title="Search" @click=${() => this.dispatchEvent(new CustomEvent('open-search', { bubbles: true, composed: true }))}>
            <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" viewBox="0 0 24 24" fill="currentColor"><path d="M10 2a8 8 0 105.293 14.293l4.707 4.707 1.414-1.414-4.707-4.707A8 8 0 0010 2zm0 2a6 6 0 110 12 6 6 0 010-12z"/></svg>
          </button>
          <div class="dropdown dropdown-end">
            <div tabindex="0" role="button" class="btn btn-ghost btn-circle" aria-label="Menu">
              <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" viewBox="0 0 24 24" fill="currentColor"><path d="M12 7a2 2 0 110-4 2 2 0 010 4zm0 7a2 2 0 110-4 2 2 0 010 4zm0 7a2 2 0 110-4 2 2 0 010 4z"/></svg>
            </div>
            <ul tabindex="0" class="menu menu-sm dropdown-content bg-base-100 rounded-box z-[1] mt-3 w-52 p-2 shadow">
              <li><a href="/settings" data-navigo @click=${this.openSettings}>Settings</a></li>
              <li><a href="/about" data-navigo>About</a></li>
              <li><a href="/sources" data-navigo>Sources</a></li>
            </ul>
          </div>
        </div>
      </div>`;
  }
}
