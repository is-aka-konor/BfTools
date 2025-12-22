import { html, type TemplateResult } from 'lit';
import type { ClassEntry } from '../data/repo';
import { loadingSpinner, sourceBadges } from './ui';

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
      <div style="margin-bottom: var(--space-xl);">
         <a class="btn btn-ghost btn-sm gap-2" href="/classes" data-navigo @click=${onBack}>
           ← Вернуться к классам
         </a>
      </div>

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

      <section class="class-description-section">
        <h2 class="class-section-title">Описание</h2>
        <div class="class-full-description" .innerHTML=${item.description ?? ''}></div>
      </section>

      <div style="margin-top: var(--space-2xl); text-align: center;">
        <a class="btn btn-secondary" href="/classes" data-navigo @click=${onBack}>
          ← Вернуться к списку классов
        </a>
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
  const levelFeatures = pickLevelFeatures(item.levels);
  const equipment = (item.startingEquipment ?? []).slice(0, 3);
  const description = buildDescriptionExcerpt(item.description ?? '');
  const subclasses = extractSubclasses(item.description ?? '');
  const icon = pickClassIcon(item.slug, item.name);

  return html`
    <a
      class="class-card block"
      href="/classes/${item.slug}"
      data-navigo
      @click=${() => onOpen(item)}
    >
      <div class="class-header">
        <div class="class-icon" aria-hidden="true">${icon}</div>
        <div class="class-title">
          <h2 class="class-name">${item.name}</h2>
          <div class="class-subtitle">Кость хитов: ${hitDie}</div>
        </div>
        <span class="class-hit-die badge badge-accent badge-lg font-mono">${hitDie}</span>
      </div>

      <div class="class-stats">
        ${renderStat('Доспехи', armor)}
        ${renderStat('Оружие', weapons)}
        ${renderStat('Инструменты', tools)}
        ${renderStat('Навыки', skillSummary)}
        ${renderStat('Спасброски', saves)}
      </div>

      ${description ? html`
        <div class="class-description">${description}</div>
      ` : null}

      ${levelFeatures.length ? html`
        <div class="class-features">
          <h4>Особенности 1 уровня</h4>
          <div class="feature-list">
            ${levelFeatures.map(f => html`<span class="feature-tag">${f}</span>`)}
          </div>
        </div>
      ` : null}

      ${subclasses.length ? html`
        <div class="class-features">
          <h4>Подклассы</h4>
          <div class="feature-list">
            ${subclasses.map(sc => html`<span class="feature-tag">${sc}</span>`)}
          </div>
        </div>
      ` : null}

      ${equipment.length ? html`
        <div class="class-equipment">
          <h4>Стартовое снаряжение</h4>
          <ul>
            ${equipment.map(eq => html`<li>${eq}</li>`)}
          </ul>
        </div>
      ` : null}

      <div class="class-footer">
        <div class="class-sources">${sourceBadges(item.sources)}</div>
        <span class="class-link">Подробнее →</span>
      </div>
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

function pickLevelFeatures(levels?: Array<{ level: number; features?: string[] }>): string[] {
  if (!levels || levels.length === 0) return [];
  const levelOne = levels.find(l => l.level === 1) ?? levels[0];
  return (levelOne.features ?? []).slice(0, 3);
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

function buildDescriptionExcerpt(description: string): string | undefined {
  if (!description) return undefined;
  const text = stripHtml(description).replace(/\s+/g, ' ').trim();
  if (!text) return undefined;
  return text.length > 220 ? `${text.slice(0, 220)}…` : text;
}

function stripHtml(input: string): string {
  return input.replace(/<[^>]+>/g, ' ').replace(/\s+/g, ' ').trim();
}

function extractSubclasses(description: string): string[] {
  if (!description) return [];
  const matches = new Set<string>();
  const plain = description
    .replace(/<[^>]+>/g, '\n')
    .split(/\n+/)
    .map(line => line.trim())
    .filter(Boolean);
  for (const line of plain) {
    const normalized = line.replace(/^#+\s*/, '');
    const m = normalized.match(/^ПОДКЛАСС:?\s*(.+)$/i);
    if (m?.[1]) {
      matches.add(capitalize(m[1]));
    }
  }
  return Array.from(matches).slice(0, 4);
}

function capitalize(value: string): string {
  const trimmed = value.trim();
  if (!trimmed) return value;
  return trimmed.charAt(0).toUpperCase() + trimmed.slice(1);
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
