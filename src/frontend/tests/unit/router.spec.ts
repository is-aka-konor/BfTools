import { describe, it, expect } from 'vitest';
import { fireEvent, waitFor } from '@testing-library/dom';
import '../../src/main';

function getRoot(): HTMLElement {
  return document.querySelector('app-root') as HTMLElement;
}

async function mountAt(path = '/') {
  history.replaceState(null, '', path);
  document.body.innerHTML = '<app-root></app-root>';
  // wait for initial render
  await (getRoot() as any).updateComplete;
}

function go(path: string) {
  history.pushState(null, '', path);
  window.dispatchEvent(new PopStateEvent('popstate'));
}

describe('Router & Navigation (jsdom)', () => {
  it('navigates main routes and renders expected hosts', async () => {
    await mountAt('/');
    const root = getRoot();

    // Home should render
    await waitFor(() => {
      const h1 = (root.shadowRoot as ShadowRoot).querySelector('h1');
      expect(h1?.textContent).toContain('Home');
    });

    // Wait for manifest counts to load (ensures sync completed)
    await waitFor(() => {
      const spellsTile = (root.shadowRoot as ShadowRoot).querySelector('a[href="/spells"] .badge');
      expect(spellsTile?.textContent?.trim()).toBeTruthy();
    });

    // Navigate to /spells
    go('/spells');

    await waitFor(() => {
      const h1 = (root.shadowRoot as ShadowRoot).querySelector('h1');
      expect(h1?.textContent).toContain('Spells');
    });

    // Navigate to /talents
    go('/talents');

    await waitFor(() => {
      const h1 = (root.shadowRoot as ShadowRoot).querySelector('h1');
      expect(h1?.textContent).toContain('Talents');
    });
  });

  it('back/forward preserves talents filter state', async () => {
    await mountAt('/');
    const root = getRoot();

    // Ensure data loaded
    await waitFor(() => {
      const badge = (root.shadowRoot as ShadowRoot).querySelector('a[href="/talents"] .badge');
      expect(badge?.textContent?.trim()).toBeTruthy();
    });

    // Go to talents
    go('/talents');

    await waitFor(() => {
      const h1 = (root.shadowRoot as ShadowRoot).querySelector('h1');
      expect(h1?.textContent).toContain('Talents');
    });

    const host = root.shadowRoot as ShadowRoot;

    // Toggle Magical off
    const magicalToggle = host.querySelector('input.toggle') as HTMLInputElement;
    expect(magicalToggle).toBeTruthy();
    if (magicalToggle.checked) {
      fireEvent.change(magicalToggle, { target: { checked: false } });
    }

    // Expect fewer results (only Martial remain)
    await waitFor(() => {
      const cards = host.querySelectorAll('a.app-card');
      expect(cards.length).toBeGreaterThan(0);
      expect(magicalToggle.checked).toBe(false);
    });

    // Navigate to a talent detail (click first card)
    const firstCard = host.querySelector('a.app-card') as HTMLAnchorElement;
    go(firstCard.getAttribute('href')!);

    await waitFor(() => {
      const h2 = (root.shadowRoot as ShadowRoot).querySelector('h2');
      expect(h2?.textContent?.trim()).toBeTruthy();
    });

    // Back to talents list
    history.back();

    await waitFor(() => {
      const h1 = (root.shadowRoot as ShadowRoot).querySelector('h1');
      expect(h1?.textContent).toContain('Talents');
      const toggle = (root.shadowRoot as ShadowRoot).querySelector('input.toggle') as HTMLInputElement;
      expect(toggle.checked).toBe(false);
    });
  });

  it('deep link renders detail from dataset', async () => {
    // Load list and navigate to first item
    await mountAt('/spells');
    const root = getRoot();
    await waitFor(() => {
      const h1 = (root.shadowRoot as ShadowRoot).querySelector('h1');
      expect(h1?.textContent).toContain('Spells');
    });
    const host = root.shadowRoot as ShadowRoot;
    await waitFor(() => expect(host.querySelectorAll('a.app-card').length).toBeGreaterThan(0));
    const first = host.querySelector('a.app-card') as HTMLAnchorElement;
    const href = first.getAttribute('href')!;
    history.pushState(null, '', href);
    window.dispatchEvent(new PopStateEvent('popstate'));
    await waitFor(() => {
      const h2 = (root.shadowRoot as ShadowRoot).querySelector('h2');
      const article = (root.shadowRoot as ShadowRoot).querySelector('article');
      expect(h2?.textContent?.trim()).toBeTruthy();
      expect((article?.innerHTML ?? '').length).toBeGreaterThan(0);
    });
  });
});
