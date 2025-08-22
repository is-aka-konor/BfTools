import { describe, it, expect, vi } from 'vitest';
import { fireEvent } from '@testing-library/dom';
import '../../src/components/AppNavbar';

describe('app-header (app-navbar)', () => {
  it('renders brand and search icon; / opens search', async () => {
    document.body.innerHTML = '<app-navbar></app-navbar>';
    const el = document.querySelector('app-navbar')!;
    expect(el).toBeInTheDocument();
    // brand link
    await (el as any).updateComplete;
    expect(el.shadowRoot!.textContent).toContain('BfTools');
    // search button present
    const btn = el.shadowRoot!.querySelector('button[title="Search"]');
    expect(btn).toBeTruthy();
    // listen for open-search
    const spy = vi.fn();
    el.addEventListener('open-search', spy);
    // press /
    fireEvent.keyDown(el, { key: '/' });
    expect(spy).toHaveBeenCalled();
  });
});
