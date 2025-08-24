import { css, html, LitElement } from 'lit';
import { customElement } from 'lit/decorators.js';

@customElement('app-drawer')
export class AppDrawer extends LitElement {
  static styles = css`:host{display:block}`;
  protected createRenderRoot() { return this; }

  render() {
    return html`
      <div class="drawer lg:drawer-open">
        <input id="app-drawer" type="checkbox" class="drawer-toggle" />
        <div class="drawer-content">
          <slot></slot>
        </div>
        <div class="drawer-side">
          <label for="app-drawer" aria-label="close sidebar" class="drawer-overlay"></label>
          <ul class="menu bg-base-200 text-base-content min-h-full w-72 p-4">
            <li class="menu-title">Browse</li>
            <li><a href="/" data-navigo>Home</a></li>
            <li><a href="/intro" data-navigo>Intro</a></li>
            <li><a href="/spellcasting" data-navigo>Spellcasting</a></li>
            <li class="menu-title mt-2">Categories</li>
            <li><a href="/classes" data-navigo>Classes</a></li>
            <li><a href="/talents" data-navigo>Talents</a></li>
            <li><a href="/lineages" data-navigo>Lineages</a></li>
            <li><a href="/backgrounds" data-navigo>Backgrounds</a></li>
            <li><a href="/spells" data-navigo>Spells</a></li>
          </ul>
        </div>
      </div>`;
  }
}
