import { html, type TemplateResult } from 'lit';
import { shell, tile } from './ui';

export function renderHome(counts: Record<string, number | undefined>): TemplateResult {
  return html`
    ${shell('Home')}
    <div class="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-6 gap-4 mt-4">
      ${tile('/intro', 'Основные правила')}
      ${tile('/spellcasting', 'Использование заклинаний')}
      ${tile('/classes', 'Классы', counts['classes'])}
      ${tile('/talents', 'Таланты', counts['talents'])}
      ${tile('/lineages', 'Происхождение', counts['lineages'])}
      ${tile('/spells', 'Заклинание', counts['spells'])}
      ${tile('/backgrounds', 'Предистории', counts['backgrounds'])}
    </div>
  `;
}

