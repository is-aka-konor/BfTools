import { html, type TemplateResult } from 'lit';

export function renderHome(counts: Record<string, number | undefined>): TemplateResult {
  return html`
    <section class="quick-nav">
      <h2>Быстрый доступ</h2>
      <div class="quick-nav-grid">
        <a class="quick-nav-card" data-navigo href="/spells">
          <div class="card-icon">✨</div>
          <h3>Заклинания</h3>
          <p>Изучите магические заклинания всех уровней и школ</p>
          <div class="card-stats"><span>${counts['spells'] ?? ''} заклинаний</span></div>
        </a>
        <a class="quick-nav-card" data-navigo href="/classes">
          <div class="card-icon">⚔️</div>
          <h3>Классы</h3>
          <p>Познакомьтесь с различными классами персонажей</p>
          <div class="card-stats"><span>${counts['classes'] ?? ''} классов</span></div>
        </a>
        <div class="quick-nav-card disabled">
          <div class="card-icon">🏛️</div>
          <h3>Школы магии</h3>
          <p>Узнайте о различных школах магического искусства</p>
          <div class="card-stats"><span>-</span></div>
        </div>
      </div>
    </section>
  `;
}
