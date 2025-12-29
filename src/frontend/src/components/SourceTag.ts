import { css, html, LitElement } from 'lit';
import { customElement } from 'lit/decorators.js';

@customElement('source-tag')
export class SourceTag extends LitElement {
  static styles = css`:host{display:inline-block}`;
  protected createRenderRoot() { return this; }
  static properties = {
    abbr: { type: String },
    name: { type: String },
  } as any;
  declare abbr: string;
  declare name: string;
  constructor() {
    super();
    // dynamic properties to avoid class field shadowing in tests
    (this as any).abbr = '';
    (this as any).name = '';
  }
  render() {
    const label = `${this.abbr} – ${this.name}`.trim();
    return html`
      <div class="tooltip" data-tip=${this.name} role="img" aria-label=${label}>
        <span class="badge badge-outline badge-sm">${this.abbr}</span>
      </div>`;
  }
}
