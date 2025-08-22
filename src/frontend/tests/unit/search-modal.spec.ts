import { describe, it, expect } from 'vitest';
import { fireEvent } from '@testing-library/dom';
import '../../src/components/SearchModal';

describe('search-modal', () => {
  it('open/close and ESC behavior; query event', async () => {
    document.body.innerHTML = '<search-modal></search-modal>';
    const el = document.querySelector('search-modal') as any;
    expect(el.open).toBeFalsy();
    await el.show();
    expect(el.open).toBeTruthy();
    // input focused
    const input = el.shadowRoot.querySelector('input[name="q"]');
    expect(document.activeElement === input || el.shadowRoot.activeElement === input).toBe(true);
    // type triggers query event
    let lastQ = '';
    el.addEventListener('query', (e: CustomEvent) => { lastQ = e.detail.q; });
    fireEvent.input(input, { target: { value: 'fir' } });
    expect(lastQ).toBe('fir');
    // ESC closes
    fireEvent.keyDown(el.shadowRoot.querySelector('.modal'), { key: 'Escape' });
    expect(el.open).toBeFalsy();
  });
});

