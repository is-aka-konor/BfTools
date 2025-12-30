import { html, type TemplateResult } from 'lit';
import type { ClassEntry } from '../../../data/repo';
import { loadingSpinner, sourceBadges } from '../../../core/ui/ui-utils';

export interface ClassesRenderOpts {
  onOpenItem?: (entry: ClassEntry) => void;
}

export function renderClasses(
  items: ClassEntry[] | undefined,
  opts: ClassesRenderOpts = {}
): TemplateResult {
  if (!items) return loadingSpinner();
  const onOpen = opts.onOpenItem ?? (() => { });

  return html`
    <div id="classesGrid" class="classes-grid">
      ${items.map(item => renderClassCard(item, onOpen))}
    </div>
  `;
}

export function renderClassDetail(
  item: ClassEntry | undefined,
  slug: string | undefined,
  opts: { onBackClick?: () => void } = {}
): TemplateResult {
  if (!item || item.slug !== slug) return loadingSpinner();
  const onBack = opts.onBackClick ?? (() => { });

  const hitDie = (item.hitDie ?? '—').toUpperCase();
  const saves = joinList(item.savingThrows) ?? '—';
  const prof = item.proficiencies ?? {};
  const armor = joinList(prof.armor) ?? '—';
  const weapons = joinList(prof.weapons) ?? '—';
  const tools = joinList(prof.tools) ?? '—';
  const skills = buildSkillSummary(prof.skills) ?? '—';
  const icon = pickClassIcon(item.slug, item.name);

  return html`
    <div class="class-detail-page">
      <header class="class-detail-header">
        <div class="class-detail-icon">${icon}</div>
        <h1 class="class-detail-title">${item.name}</h1>
        <div class="class-detail-subtitle">Класс Героя</div>
        <div style="margin-top: var(--space-md);">${sourceBadges(item.sources)}</div>
      </header>

      <section class="class-meta-grid">
        <div class="meta-item">
          <div class="meta-label">Кость Хитов</div>
          <div class="meta-value">${hitDie}</div>
        </div>
        <div class="meta-item">
          <div class="meta-label">Спасброски</div>
          <div class="meta-value">${saves}</div>
        </div>
        <div class="meta-item">
          <div class="meta-label">Доспехи</div>
          <div class="meta-value">${armor}</div>
        </div>
        <div class="meta-item">
          <div class="meta-label">Оружие</div>
          <div class="meta-value">${weapons}</div>
        </div>
        <div class="meta-item">
          <div class="meta-label">Инструменты</div>
          <div class="meta-value">${tools}</div>
        </div>
        <div class="meta-item">
          <div class="meta-label">Навыки</div>
          <div class="meta-value">${skills}</div>
        </div>
      </section>

      ${item.levels?.length ? html`
        <section class="class-progression-section">
          <h2 class="class-section-title">Развитие Класса</h2>
          <div class="class-table-container">
            <table class="class-table">
              <thead>
                <tr>
                  <th style="width: 60px; text-align: center;">Ур.</th>
                  <th style="width: 80px; text-align: center;">БМ</th>
                  <th>Умения Класса</th>
                </tr>
              </thead>
              <tbody>
                ${item.levels.map(l => html`
                  <tr>
                    <td style="text-align: center;"><span class="level-badge">${l.level}</span></td>
                    <td style="text-align: center; font-family: var(--font-mono); opacity: 0.8;">
                      ${l.proficiencyBonus ?? `+${Math.floor((l.level - 1) / 4) + 2}`}
                    </td>
                    <td>
                      <div class="feature-tag-list">
                        ${l.features?.map(f => html`<span class="feature-tag">${f}</span>`)}
                      </div>
                    </td>
                  </tr>
                `)}
              </tbody>
            </table>
          </div>
        </section>
      ` : null}

      ${(item.features?.length || item.progressInfo?.length) ? html`
        <section class="class-features-section" style="margin-bottom: var(--space-2xl);">
          <h2 class="class-section-title">Классовые Умения</h2>
          <div class="flex flex-col gap-6">
            ${(() => {
              const allFeatures = [
                ...(item.features || []),
                ...(item.progressInfo || [])
              ].sort((a, b) => (a.level || 0) - (b.level || 0));

              return allFeatures.map(f => html`
                <div class="class-feature-item">
                  <h3 class="text-xl font-bold m-0 mb-2">${f.name}</h3>
                  <div class="prose text-base-content/80" .innerHTML=${f.description}></div>
                </div>
              `);
            })()}
          </div>
        </section>
      ` : null}

      ${item.subclasses?.length ? html`
        <section class="class-subclasses-section">
          <h2 class="class-section-title">Подклассы</h2>
          <div class="flex flex-col gap-8">
            ${item.subclasses.map(sc => html`
              <div class="class-subclass-item">
                 <h3 class="text-2xl font-bold text-accent mb-4 border-b border-white/10 pb-2">${sc.name}</h3>
                 <div class="prose text-base-content/80" .innerHTML=${sc.description}></div>
              </div>
            `)}
          </div>
        </section>
      ` : null}

      <div style="margin-top: var(--space-xl); text-align: center;">
        <a class="btn btn--accent-outline" href="/classes" data-navigo @click=${onBack}>← Вернуться к списку классов</a>
      </div>
    </div>
  `;
}


function renderClassCard(item: ClassEntry, onOpen: (entry: ClassEntry) => void): TemplateResult {
  const hitDie = (item.hitDie ?? '—').toUpperCase();
  const saves = joinList(item.savingThrows) ?? '—';
  const prof = item.proficiencies ?? {};
  const armor = joinList(prof.armor) ?? '—';
  const weapons = joinList(prof.weapons) ?? '—';
  const tools = joinList(prof.tools) ?? '—';
  const skillSummary = buildSkillSummary(prof.skills) ?? '—';
  const icon = pickClassIcon(item.slug, item.name);

  return html`
    <a
      class="class-card block"
      href="/classes/${item.slug}"
      data-navigo
      @click=${() => onOpen(item)}
    >
      <div class="class-header">
        <div class="class-header-left">
          <div class="class-icon" aria-hidden="true">${icon}</div>
          <h2 class="class-name">${item.name}</h2>
        </div>
        <div class="class-header-right">
          ${sourceBadges(item.sources)}
        </div>
      </div>

      <div class="class-stats">
        ${renderStat('Доспехи', armor)}
        ${renderStat('Оружие', weapons)}
        ${renderStat('Инструменты', tools)}
        ${renderStat('Навыки', skillSummary)}
        ${renderStat('Спасброски', saves)}
      </div>

      ${item.features?.length ? html`
        <div class="class-features">
          <h4>Особенности класса:</h4>
          <div class="feature-list">
            ${item.features.map(f => html`<span class="feature-tag">${f.name}</span>`)}
          </div>
        </div>
      ` : null}

      ${item.subclasses?.length ? html`
        <div class="class-features">
          <h4>Подклассы:</h4>
          <div class="feature-list">
            ${item.subclasses.map(sc => html`<span class="feature-tag">${sc.name}</span>`)}
          </div>
        </div>
      ` : null}
    </a>
  `;
}

function joinList(values?: string[]): string | undefined {
  if (!values || values.length === 0) return undefined;
  return values.join(', ');
}

function buildSkillSummary(skills?: { granted?: string[]; choose?: number; from?: string[] }): string | undefined {
  if (!skills) return undefined;
  const parts: string[] = [];
  if (skills.granted?.length) parts.push(skills.granted.join(', '));
  if (typeof skills.choose === 'number') {
    const fromList = skills.from?.length ? skills.from.join(', ') : 'списка доступных навыков';
    const chooseText = skills.choose === 1 ? 'Выберите 1' : `Выберите ${skills.choose}`;
    parts.push(`${chooseText} из: ${fromList}`);
  }
  return parts.length ? parts.join('. ') : undefined;
}

function renderStat(label: string, value: string | undefined): TemplateResult {
  if (!value || value.trim().length === 0) {
    return html``;
  }
  return html`
    <div class="stat-item">
      <span class="stat-label">${label}</span>
      <span class="stat-value">${value}</span>
    </div>
  `;
}

function pickClassIcon(slug: string | undefined, name: string | undefined): string {
  const key = (slug ?? name ?? '').toLowerCase();
  const map: Record<string, string> = {
    bard: '🎼',
    cleric: '⛨',
    fighter: '🛡️',
    ranger: '🏹',
    rogue: '🗡️',
    wizard: '🔮',
    barbarian: '🪓',
    paladin: '⚔️',
    sorcerer: '✨',
    warlock: '📜',
    monk: '🥋',
    druid: '🍃',
    mechanist: '⚙️',
  };
  return map[key] || (name ? name.charAt(0).toUpperCase() : '★');
}
