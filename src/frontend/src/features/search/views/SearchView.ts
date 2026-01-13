import { html, type TemplateResult } from 'lit';
import { loadingSpinner, shell } from '../../../core/ui/ui-utils';

export function renderSearchPage(
  query: string,
  searching: boolean,
  results: Array<{ doc: any; score: number }>,
  renderResult: (r: { doc: any; score: number }) => TemplateResult
): TemplateResult {
  return html`
    ${shell('Search', html`<p class="search-summary">Results for <code>${query || '(empty)'}</code></p>`)}
    ${searching ? loadingSpinner() : renderSearchResults(query, results, renderResult)}
  `;
}

export function renderSearchResults(
  query: string,
  results: Array<{ doc: any; score: number }>,
  renderResult: (r: { doc: any; score: number }) => TemplateResult
): TemplateResult {
  if (!query) return html`<p class="text-muted">Type in the search bar to find content.</p>`;
  if (results.length === 0) return html`<p class="text-muted">Nothing was found for “${query}”.</p>`;
  return html`<div class="result-list">${results.map(r => renderResult(r))}</div>`;
}

export function renderResult(r: { doc: any; score: number }): TemplateResult {
  const d = r.doc;
  const href = `/${d.category}/${d.slug}`;
  return html`
    <a href="${href}" data-navigo class="ui-card ui-card--interactive result-card">
      <div class="ui-card__body">
        <div class="result-card__header">
          <h3 class="ui-card__title">${d.name}</h3>
          <span class="ui-badge ui-badge--sm">${d.category}</span>
        </div>
        ${(d as any).description ? html`<p class="result-card__desc line-clamp-2" .innerHTML=${(d as any).description}></p>` : ''}
      </div>
    </a>
  `;
}
