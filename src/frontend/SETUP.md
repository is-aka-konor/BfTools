# Setup & Development Guide

This document provides instructions on how to build, run, and test the Tales of Valiant frontend.

## Prerequisites

-   **Node.js**: v18 or higher.
-   **npm**: v9 or higher.

## Getting Started

1.  **Install Dependencies**:

    ```bash
    npm install
    ```

2.  **Start Development Server**:

    ```bash
    npm run dev
    ```

    The app will be available at `http://localhost:5173`.

3.  **Build for Production**:
    ```bash
    npm run build
    ```
    The static assets will be generated in the `dist/` directory.

---

## Testing

We use **Vitest** for unit testing and **Playwright** for end-to-end testing.

### Unit Tests

Verify logic, store behavior, and small component parts.

```bash
# Run once
npm run test:unit

# Watch mode
npm run test:unit:watch

# With coverage
npm run test:unit -- --coverage
```

### E2E Tests

Verify full user flows and offline functionality using real browser automation.

```bash
npm run test:e2e
```

---

## Development Workflow

1.  **Style Guide**: Custom styles live in `src/style.css`. Update shared tokens and utility classes there before adding component-specific CSS.
2.  **Components**: Use **Lit** for building web components. Avoid adding large external JS libraries.
3.  **Sync Data**: If you are working on data extraction (backend), ensure you update the `site-manifest.json` so the frontend can detect changes.
4.  **Preview Build**:
    ```bash
    npm run preview
    ```
    This serves the `dist/` folder for final verification before deployment.
