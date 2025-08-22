import type { Config } from 'tailwindcss';
import daisyui from 'daisyui';

export default {
  content: [
    './index.html',
    './src/**/*.{ts,tsx,js,jsx,html}'
  ],
  theme: {
    extend: {}
  },
  daisyui: {
    themes: [
      {
        totv: {
          primary: '#2563eb',
          'primary-content': '#ffffff',
          secondary: '#0ea5e9',
          accent: '#22d3ee',
          neutral: '#1f2937',
          'base-100': '#0b1020',
          'base-200': '#0f172a',
          'base-300': '#111827',
          info: '#06b6d4',
          success: '#16a34a',
          warning: '#f59e0b',
          error: '#ef4444',
        }
      },
      {
        totvDark: {
          primary: '#60a5fa',
          'primary-content': '#0b1020',
          secondary: '#38bdf8',
          accent: '#67e8f9',
          neutral: '#111827',
          'base-100': '#0b1020',
          'base-200': '#0a0f1a',
          'base-300': '#0a0e16',
          info: '#22d3ee',
          success: '#4ade80',
          warning: '#fbbf24',
          error: '#f87171',
        }
      }
    ],
    darkTheme: 'totvDark'
  },
  plugins: [daisyui]
} satisfies Config;
