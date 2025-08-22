import '@testing-library/jest-dom/vitest';
import 'fake-indexeddb/auto';

// ResizeObserver/IntersectionObserver shims
class ResizeObserver {
  observe() { /* noop */ }
  unobserve() { /* noop */ }
  disconnect() { /* noop */ }
}
// @ts-ignore
global.ResizeObserver = ResizeObserver;

class IntersectionObserver {
  constructor() {}
  observe() {}
  unobserve() {}
  disconnect() {}
  takeRecords() { return []; }
}
// @ts-ignore
global.IntersectionObserver = IntersectionObserver;

// MSW server lifecycle
// Disable Lit dev mode checks for tests to avoid class-field shadowing warnings as errors
// @ts-ignore
globalThis.litDisableDevMode = true;
import { server } from './msw/server';
import { beforeAll, afterAll, afterEach } from 'vitest';
beforeAll(() => server.listen({ onUnhandledRequest: 'bypass' }));
afterEach(() => server.resetHandlers());
afterAll(() => server.close());
