import { html, type TemplateResult } from 'lit';

export interface LayoutParams {
  routeName: string;
  content: TemplateResult;
  counts: Record<string, number | undefined>;
  sidebarOpen: boolean;
  onToggleSidebar: () => void;
  onSearch: (q: string) => void;
  breadcrumbs?: Array<{ label: string; href?: string }>;
}

export function renderLayout(p: LayoutParams): TemplateResult {
  const { routeName, content, sidebarOpen, onToggleSidebar, counts } = p;
  const onKey = (e: KeyboardEvent) => {
    if (e.key === 'Enter') {
      const q = (e.target as HTMLInputElement).value;
      p.onSearch(q);
    }
  };
  const link = (href: string, icon: string, label: string) => html`
    <a href="${href}" data-navigo class="sidebar-link" @click=${() => onToggleSidebar()}>
      <span class="sidebar-icon">${icon}</span>
      ${label}
    </a>`;

  const homeQuick = html`
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
    </section>`;

  return html`
    <!-- Navigation -->
    <nav class="navbar">
      <div class="nav-content">
        <div class="nav-brand">
          <h1>Tales of the Valiant</h1>
          <span class="nav-subtitle">Справочник RPG</span>
        </div>
        <button class="mobile-menu-btn" @click=${onToggleSidebar} aria-label="Toggle menu">
          <span></span><span></span><span></span>
        </button>
      </div>
    </nav>

    <!-- Sidebar -->
    <aside class="sidebar ${sidebarOpen ? 'open' : ''}">
      <div class="sidebar-content">
        <nav class="sidebar-nav">
          ${link('/', '🏠', 'Главная')}
          ${link('/spells', '✨', 'Заклинания')}
          ${link('/classes', '⚔️', 'Классы')}
          ${link('/talents', '🎖️', 'Таланты')}
          ${link('/lineages', '🧬', 'Происхождения')}
          ${link('/backgrounds', '📜', 'Предыстории')}
        </nav>
        <div class="sidebar-search">
          <input type="text" placeholder="Поиск..." class="form-control search-input" @keydown=${onKey} />
          <div class="search-results" hidden></div>
        </div>
      </div>
    </aside>

    <!-- Main Content -->
    <main class="main-content">
      <nav class="breadcrumbs">
        ${
          (p.breadcrumbs && p.breadcrumbs.length > 0)
            ? p.breadcrumbs.map((c, i) => (
                (c.href && i < p.breadcrumbs!.length - 1)
                  ? html`<a class="breadcrumb-item" href="${c.href}" data-navigo>${c.label}</a>`
                  : html`<span class="breadcrumb-item">${c.label}</span>`
              ))
            : html`<span class="breadcrumb-item">${routeName}</span>`
        }
      </nav>
      ${routeName === 'home' ? html`
        <section class="hero">
          <div class="hero-content">
            <h1 class="hero-title">Добро пожаловать в Tales of the Valiant</h1>
            <p class="hero-description">Полный справочник по правилам игры. Исследуйте заклинания, классы персонажей и магические школы.</p>
          </div>
        </section>
        ${homeQuick}
      ` : null}
      <div class="page active">
        ${content}
      </div>
    </main>

    <!-- Overlay for mobile -->
    <div class="overlay ${sidebarOpen ? 'open' : ''}" @click=${onToggleSidebar}></div>
  `;
}
