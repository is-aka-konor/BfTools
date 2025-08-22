import { describe, it, expect, beforeEach } from 'vitest';
import { waitFor, fireEvent } from '@testing-library/dom';
import '../../src/main';

async function mountAt(path = '/talents') {
  history.replaceState(null, '', path);
  document.body.innerHTML = '<app-root></app-root>';
  await (document.querySelector('app-root') as any).updateComplete;
}

function rootSR(): ShadowRoot { return (document.querySelector('app-root') as any).shadowRoot as ShadowRoot; }

describe('Talents Filters (integration)', () => {
  beforeEach(async () => {
    await mountAt('/');
    await waitFor(() => {
      const badge = rootSR().querySelector('a[href="/talents"] .badge');
      expect(badge?.textContent?.trim()).toBeTruthy();
    });
    history.pushState(null, '', '/talents');
    window.dispatchEvent(new PopStateEvent('popstate'));
    await waitFor(() => expect(rootSR().querySelector('h1')?.textContent).toContain('Talents'));
  });

  it('magical/martial toggles and multi-source OR', async () => {
    const host = rootSR();
    // default both on => > 0
    await waitFor(() => expect(host.querySelectorAll('a.app-card').length).toBeGreaterThan(0));

    // Magical off => only Martial remain (3 from fixtures)
    const magicalToggle = host.querySelectorAll('input.toggle')[0] as HTMLInputElement;
    fireEvent.change(magicalToggle, { target: { checked: false } });
    await waitFor(() => expect(host.querySelectorAll('a.app-card').length).toBe(3));

    // Select BF source chip then TC -> union grows to all (6)
    const chips = Array.from(host.querySelectorAll('button.btn.btn-xs')) as HTMLButtonElement[];
    fireEvent.click(chips[0]); // BF
    await waitFor(() => expect(host.querySelectorAll('a.app-card').length).toBeGreaterThan(0));
    fireEvent.click(chips[1]); // TC
    await waitFor(() => expect(host.querySelectorAll('a.app-card').length).toBe(3)); // still Martial-only

    // Enable Magical back => full union 6
    fireEvent.change(magicalToggle, { target: { checked: true } });
    await waitFor(() => expect(host.querySelectorAll('a.app-card').length).toBe(6));
  });

  it('reset behavior and persistence on back', async () => {
    const host = rootSR();
    // Toggle Martial off (Magical only)
    const martialToggle = host.querySelectorAll('input.toggle')[1] as HTMLInputElement;
    fireEvent.change(martialToggle, { target: { checked: false } });
    await waitFor(() => expect(host.querySelectorAll('a.app-card').length).toBe(3));

    const first = host.querySelector('a.app-card') as HTMLAnchorElement;
    history.pushState(null, '', first.getAttribute('href')!);
    window.dispatchEvent(new PopStateEvent('popstate'));
    await waitFor(() => expect(rootSR().querySelector('h2')?.textContent).toBeTruthy());
    history.back();
    await waitFor(() => expect((rootSR().querySelectorAll('a.app-card').length)).toBe(3));
  });
});

