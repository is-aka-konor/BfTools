import { describe, it, expect, beforeEach } from 'vitest';
import { waitFor, fireEvent } from '@testing-library/dom';
import '../../src/main';

async function mountAt(path = '/spells') {
  history.replaceState(null, '', path);
  document.body.innerHTML = '<app-root></app-root>';
  await (document.querySelector('app-root') as any).updateComplete;
}

function rootSR(): ShadowRoot { return (document.querySelector('app-root') as any).shadowRoot as ShadowRoot; }

describe('Spells Filters (integration)', () => {
  beforeEach(async () => {
    await mountAt('/');
    // Ensure sync done
    await waitFor(() => {
      const badge = rootSR().querySelector('a[href="/spells"] .badge');
      expect(badge?.textContent?.trim()).toBeTruthy();
    });
    history.pushState(null, '', '/spells');
    window.dispatchEvent(new PopStateEvent('popstate'));
    await waitFor(() => expect(rootSR().querySelector('h1')?.textContent).toContain('Spells'));
  });

  it('facets individually work and counts match', async () => {
    const host = rootSR();
    // Circle=1
    const circleSel = host.querySelector('select.select') as HTMLSelectElement;
    circleSel.value = '1';
    fireEvent.change(circleSel);
    await waitFor(() => expect(host.querySelectorAll('a.app-card').length).toBe(1));

    // School=Evocation (reset circle first)
    circleSel.value = '';
    fireEvent.change(circleSel);
    const schoolSel = host.querySelectorAll('select.select')[1] as HTMLSelectElement;
    schoolSel.value = 'Evocation';
    fireEvent.change(schoolSel);
    await waitFor(() => expect(host.querySelectorAll('a.app-card').length).toBe(2));

    // Reset school, then Ritual=true
    schoolSel.value = '';
    fireEvent.change(schoolSel);
    const ritualToggle = host.querySelector('input.toggle') as HTMLInputElement;
    fireEvent.change(ritualToggle, { target: { checked: true } });
    await waitFor(() => expect(host.querySelectorAll('a.app-card').length).toBe(3));

    // CircleType=Divine
    // Reset Ritual to neutral
    fireEvent.change(ritualToggle, { target: { checked: false } });
    const ctSel = host.querySelectorAll('select.select')[2] as HTMLSelectElement;
    ctSel.value = 'Divine';
    fireEvent.change(ctSel);
    await waitFor(() => expect(host.querySelectorAll('a.app-card').length).toBe(4));
  });

  it('composition and clear-all', async () => {
    const host = rootSR();
    const circleSel = host.querySelector('select.select') as HTMLSelectElement;
    const schoolSel = host.querySelectorAll('select.select')[1] as HTMLSelectElement;
    const ritualToggle = host.querySelector('input.toggle') as HTMLInputElement;

    circleSel.value = '1'; fireEvent.change(circleSel);
    schoolSel.value = 'Abjuration'; fireEvent.change(schoolSel);
    fireEvent.change(ritualToggle, { target: { checked: true } });
    await waitFor(() => expect(host.querySelectorAll('a.app-card').length).toBe(1));

    // Clear all
    const clearBtn = Array.from(host.querySelectorAll('button')).find(b => (b as HTMLButtonElement).textContent?.includes('Clear All')) as HTMLButtonElement;
    fireEvent.click(clearBtn);
    await waitFor(() => expect(host.querySelectorAll('a.app-card').length).toBe(8));
  });

  it('sources OR and sorting', async () => {
    const host = rootSR();
    // Select BF source badge (first chip)
    const chips = Array.from(host.querySelectorAll('button.btn.btn-xs')) as HTMLButtonElement[];
    fireEvent.click(chips[0]);
    await waitFor(() => expect(host.querySelectorAll('a.app-card').length).toBeGreaterThan(0));

    // Select TC too (should expand, no duplicates)
    fireEvent.click(chips[1]);
    const slugs = Array.from(host.querySelectorAll('a.app-card')).map(a => (a as HTMLAnchorElement).getAttribute('href'));
    expect(new Set(slugs).size).toBe(slugs.length);

    // Sort by name asc => first Gate
    const sortSel = host.querySelectorAll('select.select')[3] as HTMLSelectElement;
    sortSel.value = 'name-asc'; fireEvent.change(sortSel);
    await waitFor(() => expect((host.querySelector('.card-title') as HTMLElement).textContent).toBe('Gate'));
    // name desc => first Ward
    sortSel.value = 'name-desc'; fireEvent.change(sortSel);
    await waitFor(() => expect((host.querySelector('.card-title') as HTMLElement).textContent).toBe('Ward'));
    // circle asc => first Spark; circle desc => first Storm
    sortSel.value = 'circle-asc'; fireEvent.change(sortSel);
    await waitFor(() => expect((host.querySelector('.card-title') as HTMLElement).textContent).toBe('Spark'));
    sortSel.value = 'circle-desc'; fireEvent.change(sortSel);
    await waitFor(() => expect((host.querySelector('.card-title') as HTMLElement).textContent).toBe('Storm'));
  });

  it('filter state persists on route change', async () => {
    const host = rootSR();
    // Set a school filter
    const schoolSel = host.querySelectorAll('select.select')[1] as HTMLSelectElement;
    schoolSel.value = 'Evocation'; fireEvent.change(schoolSel);
    await waitFor(() => expect(host.querySelectorAll('a.app-card').length).toBe(2));

    // Navigate to detail and back
    const first = host.querySelector('a.app-card') as HTMLAnchorElement;
    history.pushState(null, '', first.getAttribute('href')!);
    window.dispatchEvent(new PopStateEvent('popstate'));
    await waitFor(() => expect(rootSR().querySelector('h2')?.textContent).toBeTruthy());
    history.back();
    await waitFor(() => expect(rootSR().querySelectorAll('a.app-card').length).toBe(2));
  });
});
