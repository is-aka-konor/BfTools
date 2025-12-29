import Navigo from 'navigo';
import { metaService, type RouteMeta } from '../services/MetaService';

export type RouteHandler = (params: any, query: string) => void | Promise<void>;

export class AppRouter {
  private router: Navigo;
  
  constructor() {
    this.router = new Navigo('/');
  }

  on(path: string, handler: RouteHandler, meta?: RouteMeta) {
    this.router.on(path, async (match) => {
      if (meta) metaService.update(meta);
      const params = match?.data || match?.params || {};
      const query = match?.queryString || '';
      await handler(params, query);
    });
    return this;
  }

  notFound(handler: () => void) {
    this.router.notFound(() => {
      metaService.update({ title: 'Not Found' });
      handler();
    });
    return this;
  }

  navigate(path: string) {
    this.router.navigate(path);
  }

  resolve() {
    this.router.resolve();
  }

  // Helper to attach to global click events for data-navigo
  setupLinkDelegation() {
    document.body.addEventListener('click', (e) => {
      const target = (e.target as HTMLElement).closest('[data-navigo]');
      if (target && target instanceof HTMLAnchorElement) {
        e.preventDefault();
        const href = target.getAttribute('href');
        if (href) this.navigate(href);
      }
    });
  }
}

export const appRouter = new AppRouter();
