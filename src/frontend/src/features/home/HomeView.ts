import { html, type TemplateResult } from 'lit';

export function renderHome(counts: Record<string, number | undefined>): TemplateResult {
  return html`
    <section class="quick-nav">
      <h2>Быстрый доступ</h2>
      <div class="quick-nav-grid">
        <a class="quick-nav-card" data-navigo href="/spells">
          <div class="card-icon">✨</div>
          <h3 class="list-card-name">Заклинания</h3>
          <p>Изучите магические заклинания всех уровней и школ</p>
          <div class="card-stats"><span>${counts['spells'] ?? ''} заклинаний</span></div>
        </a>
        <a class="quick-nav-card" data-navigo href="/classes">
          <div class="card-icon">⚔️</div>
          <h3 class="list-card-name">Классы</h3>
          <p>Познакомьтесь с различными классами персонажей</p>
          <div class="card-stats"><span>${counts['classes'] ?? ''} классов</span></div>
        </a>
        <a class="quick-nav-card" data-navigo href="/talents">
          <div class="card-icon">🎖️</div>
          <h3 class="list-card-name">Таланты</h3>
          <p>Выберите уникальные способности для вашего персонажа</p>
          <div class="card-stats"><span>${counts['talents'] ?? ''} талантов</span></div>
        </a>
        <a class="quick-nav-card" data-navigo href="/lineages">
          <div class="card-icon">🧬</div>
          <h3 class="list-card-name">Происхождения</h3>
          <p>Исследуйте различные расы и народы мира</p>
          <div class="card-stats"><span>${counts['lineages'] ?? ''} происхождений</span></div>
        </a>
        <a class="quick-nav-card" data-navigo href="/backgrounds">
          <div class="card-icon">📜</div>
          <h3 class="list-card-name">Предыстории</h3>
          <p>Определите прошлое вашего героя и его жизненный опыт</p>
          <div class="card-stats"><span>${counts['backgrounds'] ?? ''} предысторий</span></div>
        </a>
      </div>
    </section>
  `;
}
