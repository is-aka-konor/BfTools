import { html, type TemplateResult } from 'lit';

export function shell(title: string, extra?: unknown): TemplateResult {
  return html`
    <div class="prose prose--wide">
      ${title ? html`<h1>${title}</h1>` : ''}
      ${extra ?? ''}
    </div>
  `;
}

export function loadingSpinner(): TemplateResult {
  return html`<span class="ui-spinner" aria-hidden="true"></span>`;
}

export function sourceBadges(sources: Array<{ abbr: string; name: string }> | undefined): TemplateResult {
  return html`${sources?.map(s => html`
    <div class="ui-tooltip" data-tip=${s.name}>
      <span class="ui-badge ui-badge--outline ui-badge--sm ui-badge--source">${s.abbr}</span>
    </div>`)}
  `;
}
