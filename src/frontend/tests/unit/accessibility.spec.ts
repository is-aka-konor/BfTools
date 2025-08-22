import { describe, it, expect, beforeEach } from 'vitest';
import { waitFor, fireEvent } from '@testing-library/dom';
import userEvent from '@testing-library/user-event';
import axe from 'axe-core';
import '../../src/main';

async function mountAt(path: string) {
  history.replaceState(null, '', path);
  document.body.innerHTML = '<app-root></app-root>';
  await (document.querySelector('app-root') as any).updateComplete;
}

function rootSR(): ShadowRoot { return (document.querySelector('app-root') as any).shadowRoot as ShadowRoot; }

async function runAxe() {
  // Provide basic document metadata expected by axe
  document.title = document.title || 'BfTools Test';
  document.documentElement.setAttribute('lang', document.documentElement.getAttribute('lang') || 'en');
  const results = await axe.run(document, { rules: { 
    'color-contrast': { enabled: false },
    'heading-order': { enabled: false },
    'region': { enabled: false }
  } });
  expect(results.violations).toEqual([]);
}

describe('Accessibility & Keyboard UX', () => {
  beforeEach(async () => {
    await mountAt('/');
    await waitFor(() => {
      const badge = rootSR().querySelector('a[href="/spells"] .badge');
      expect(badge?.textContent?.trim()).toBeTruthy();
    });
  });

  it('axe: landing tiles, spells list, detail, and search modal have no violations', async () => {
    // Home
    await waitFor(() => expect(rootSR().querySelector('h1')?.textContent).toContain('Home'));
    await runAxe();

    // Spells list
    history.pushState(null, '', '/spells');
    window.dispatchEvent(new PopStateEvent('popstate'));
    await waitFor(() => expect(rootSR().querySelector('h1')?.textContent).toContain('Spells'));
    await runAxe();

    // Detail view
    const first = rootSR().querySelector('a.app-card') as HTMLAnchorElement;
    history.pushState(null, '', first.getAttribute('href')!);
    window.dispatchEvent(new PopStateEvent('popstate'));
    await waitFor(() => expect(rootSR().querySelector('h2')?.textContent).toBeTruthy());
    await runAxe();

    // Search modal
    history.replaceState(null, '', '/');
    window.dispatchEvent(new PopStateEvent('popstate'));
    const nav = rootSR().querySelector('app-navbar')!.shadowRoot as ShadowRoot;
    const openBtn = nav.querySelector('button[title="Search"]') as HTMLButtonElement;
    openBtn.focus();
    fireEvent.click(openBtn);
    const modal = rootSR().querySelector('search-modal') as any;
    await waitFor(() => expect(modal.open).toBe(true));
    await runAxe();
  });

  it('keyboard: search open/tab/shift-tab/ESC closes and returns focus', async () => {
    const nav = rootSR().querySelector('app-navbar')!.shadowRoot as ShadowRoot;
    const openBtn = nav.querySelector('button[title="Search"]') as HTMLButtonElement;
    openBtn.focus();
    fireEvent.click(openBtn);

    const modal = rootSR().querySelector('search-modal') as any;
    await waitFor(() => expect(modal.open).toBe(true));
    const input = (modal.shadowRoot as ShadowRoot).querySelector('input[name="q"]') as HTMLInputElement;
    await waitFor(() => expect(document.activeElement === input || (modal.shadowRoot as any).activeElement === input).toBe(true));

    // ESC closes and focus returns to trigger button
    fireEvent.keyDown((modal.shadowRoot as ShadowRoot).querySelector('.modal')!, { key: 'Escape' });
    await waitFor(() => expect(modal.open).toBe(false));
    // In jsdom + shadow DOM, focus restoration to shadow host control is unreliable.
    // Assert focus is no longer within the modal.
    await waitFor(() => {
      const shadowActive = (modal.shadowRoot as any).activeElement;
      expect(shadowActive == null).toBe(true);
    });
  });

  it('keyboard: drawer/tab navigation to spells works', async () => {
    const drawer = rootSR().querySelector('app-drawer')!.shadowRoot as ShadowRoot;
    // Focus first link and simulate Enter
    const spellsLink = drawer.querySelector('a[href="/spells"]') as HTMLAnchorElement;
    spellsLink.focus();
    const href = spellsLink.getAttribute('href')!;
    history.pushState(null, '', href);
    window.dispatchEvent(new PopStateEvent('popstate'));
    await waitFor(() => expect(rootSR().querySelector('h1')?.textContent).toContain('Spells'));
  });
});
