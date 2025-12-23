import { html, type TemplateResult } from 'lit';

export function shell(title: string, extra?: unknown): TemplateResult {
  return html`
    <div class="prose max-w-none">
      ${title ? html`<h1>${title}</h1>` : ''}
      ${extra ?? ''}
    </div>
  `;
}

export function loadingSpinner(): TemplateResult {
  return html`<span class="loading loading-spinner"></span>`;
}

export function sourceBadges(sources: Array<{ abbr: string; name: string }> | undefined): TemplateResult {
  return html`${sources?.map(s => html`
    <div class="tooltip" data-tip=${s.name}>
      <span class="badge--source">${s.abbr}</span>
    </div>`)}
  `;
}
