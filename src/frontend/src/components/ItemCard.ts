import { css, html, LitElement } from 'lit';
import { customElement } from 'lit/decorators.js';
import './SourceTag';

@customElement('item-card')
export class ItemCard extends LitElement {
  static styles = css`:host{display:block}`;
  protected createRenderRoot() { return this; }
  static properties = {
    name: { type: String },
    slug: { type: String },
    category: { type: String },
    circle: { type: Number },
    school: { type: String },
    isRitual: { type: Boolean },
    sources: { attribute: false },
  } as any;
  constructor() {
    super();
    (this as any).name = '';
    (this as any).slug = '';
    (this as any).category = '';
    (this as any).sources = [];
  }

  private onClick = () => {
    this.dispatchEvent(new CustomEvent('navigate', { detail: { slug: (this as any).slug, category: (this as any).category }, bubbles: true, composed: true }));
  };

  render() {
    const circle = (this as any).circle;
    const school = (this as any).school;
    const isRitual = (this as any).isRitual;
    const sources = (this as any).sources as Array<{ abbr: string; name: string }>;
    const name = (this as any).name as string;
    const circleBadge = circle !== undefined ? html`<span class="badge badge-sm" aria-label="Circle ${circle}">C${circle}</span>` : '';
    const schoolBadge = school ? html`<span class="badge badge-sm" aria-label=${`School ${school}`}>${school}</span>` : '';
    const ritualBadge = isRitual ? html`<span class="badge badge-sm" aria-label="Ritual">Ritual</span>` : '';
    return html`
      <button class="app-card card text-left w-full" @click=${this.onClick} aria-label=${name}>
        <div class="card-body p-4">
          <div class="flex items-center gap-2">
            <h3 class="card-title m-0">${name}</h3>
            ${circleBadge}
            ${schoolBadge}
            ${ritualBadge}
            <div class="flex gap-1 flex-wrap ml-2">
              ${sources.map(s => html`<source-tag abbr=${s.abbr} name=${s.name}></source-tag>`)}
            </div>
          </div>
        </div>
      </button>`;
  }
}
