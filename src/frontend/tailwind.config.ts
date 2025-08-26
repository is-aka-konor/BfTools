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
    themes: [
      {
        tov: {
          primary: '#8b5cf6',
          'primary-content': '#f3e8ff',
          secondary: '#334155',
          'secondary-content': '#fbbf24',
          accent: '#fbbf24',
          'accent-content': '#1e293b',
          neutral: '#1e293b',
          'neutral-content': '#fbbf24',
          'base-100': '#0f172a',
          'base-200': '#1e293b',
          'base-300': '#334155',
          'base-content': '#f5f5dc',
          info: '#1e90ff',
          success: '#16a34a',
          warning: '#f59e0b',
          error: '#dc2626',
          '--rounded-box': '0.5rem',
          '--rounded-btn': '0.25rem',
          '--rounded-badge': '0.125rem',
          '--animation-btn': '0.25s',
          '--animation-input': '.2s',
          '--btn-focus-scale': '0.95',
          '--border-btn': '1px',
          '--tab-border': '1px',
          '--tab-radius': '0.5rem',
        },
      },
      'light',
      'dark',
    ],
    darkTheme: 'tov',
  },
} satisfies Config;
