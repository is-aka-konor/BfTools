import { describe, it, expect, vi } from 'vitest';
import { fireEvent } from '@testing-library/dom';
import '../../src/components/ItemCard';

describe('item-card', () => {
  it('renders fields and emits navigate on click', async () => {
    document.body.innerHTML = '<item-card></item-card>';
    const el = document.querySelector('item-card') as any;
    el.name = 'Fireball';
    el.slug = 'fireball';
    el.category = 'spells';
    el.circle = 3; el.school = 'Evocation'; el.isRitual = false;
    el.sources = [{ abbr: 'BF', name: 'Black Flag Core' }];
    await (el as any).updateComplete;
    const spy = vi.fn();
    el.addEventListener('navigate', spy);
    const btn = el.shadowRoot!.querySelector('button');
    expect(btn).toBeTruthy();
    fireEvent.click(btn!);
    expect(spy).toHaveBeenCalled();
    const badges = el.shadowRoot!.querySelectorAll('.badge');
    expect(badges.length).toBeGreaterThan(0);
  });
});
