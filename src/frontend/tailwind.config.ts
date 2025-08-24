import type { Config } from 'tailwindcss';

// Tailwind v4 with Vite plugin; DaisyUI is loaded via CSS @plugin.
export default {
  content: ['./index.html','./src/**/*.{ts,js,html}'],
  theme: { extend: {} }
} satisfies Config;
