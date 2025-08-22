import { describe, it, expect } from 'vitest';
import '../../src/components/SourceTag';

describe('source-tag', () => {
  it('shows abbr and tooltip; a11y label present', async () => {
    document.body.innerHTML = '<source-tag abbr="BF" name="Black Flag Core"></source-tag>';
    const el = document.querySelector('source-tag') as any;
    await el.updateComplete;
    const badg = el.shadowRoot!.querySelector('.badge') as HTMLElement;
    expect(badg.textContent).toContain('BF');
    const wrapper = el.shadowRoot!.querySelector('[role="img"]') as HTMLElement;
    expect(wrapper.getAttribute('aria-label')).toContain('Black Flag Core');
  });
});
