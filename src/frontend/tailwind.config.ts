import type { Config } from 'tailwindcss';

// Tailwind v4 with Vite plugin; DaisyUI is loaded via CSS @plugin.
export default {
  content: [
    './index.html',
    './src/**/*.{js,ts,jsx,tsx}',
  ],
  theme: { extend: {} },
  plugins: [require('daisyui')],
    daisyui: {
      themes: ['light', 'dark', 'cupcake'],
      darkTheme: 'dark',
    },
} satisfies Config;
