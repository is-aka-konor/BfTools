import { html, type TemplateResult } from 'lit';

export function shell(title: string, extra?: unknown): TemplateResult {
  return html`
    <div class="prose max-w-none">
      <h1>${title}</h1>
      ${extra ?? ''}
    </div>
  `;
}

export function tile(href: string, label: string, count?: number): TemplateResult {
  return html`
    <a href="${href}" data-navigo class="indicator app-card card hover:shadow-lg transition-shadow">
      ${typeof count === 'number' ? html`<span class="indicator-item badge badge-accent">${count}</span>` : null}
      <div class="card-body p-4">
        <h3 class="card-title">${label}</h3>
      </div>
    </a>
  `;
}

export function loadingSpinner(): TemplateResult {
  return html`<span class="loading loading-spinner"></span>`;
}

export function sourceBadges(sources: Array<{ abbr: string; name: string }> | undefined): TemplateResult {
  return html`${sources?.map(s => html`<div class="tooltip" data-tip=${s.name}><span class="badge badge-outline badge-sm">${s.abbr}</span></div>`)}`;
}

