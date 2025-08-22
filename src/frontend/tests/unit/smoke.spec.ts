import { describe, it, expect } from 'vitest';
import { screen, getByText } from '@testing-library/dom';

describe('smoke', () => {
  it('runs with jsdom', () => {
    document.body.innerHTML = '<div>hello</div>';
    expect(getByText(document.body, 'hello')).toBeInTheDocument();
  });
});

