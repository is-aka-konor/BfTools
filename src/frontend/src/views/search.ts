import { html, type TemplateResult } from 'lit';
import { loadingSpinner, shell } from './ui';

export function renderSearchPage(
  query: string,
  searching: boolean,
  results: Array<{ doc: any; score: number }>,
  renderResult: (r: { doc: any; score: number }) => TemplateResult
): TemplateResult {
  return html`
    ${shell('Search', html`<p class="mt-2">Results for <code>${query || '(empty)'}</code></p>`)}
    ${searching ? loadingSpinner() : renderSearchResults(query, results, renderResult)}
  `;
}

export function renderSearchResults(
  query: string,
  results: Array<{ doc: any; score: number }>,
  renderResult: (r: { doc: any; score: number }) => TemplateResult
): TemplateResult {
  if (!query) return html`<p class="opacity-70">Type in the search bar to find content.</p>`;
  if (results.length === 0) return html`<p class="opacity-70">Nothing was found for “${query}”.</p>`;
  return html`<div class="mt-4 space-y-3">${results.map(r => renderResult(r))}</div>`;
}

export function renderResult(r: { doc: any; score: number }): TemplateResult {
  const d = r.doc;
  const href = `/${d.category}/${d.slug}`;
  return html`
    <a href="${href}" data-navigo class="app-card block">
      <div class="card-body p-4">
        <div class="flex items-center gap-2">
          <h3 class="card-title m-0">${d.name}</h3>
          <span class="app-badge badge-sm">${d.category}</span>
        </div>
        ${(d as any).description ? html`<p class="line-clamp-2 opacity-80" .innerHTML=${(d as any).description}></p>` : ''}
      </div>
    </a>
  `;
}
