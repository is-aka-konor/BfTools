/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {
      colors: {
        // Tales of the Valiant цветовая схема
        'tov-dark': {
          900: '#0f172a', // slate-900
          800: '#1e293b', // slate-800
          700: '#334155', // slate-700
          600: '#475569', // slate-600
          500: '#64748b', // slate-500
        },
        'tov-accent': {
          400: '#fbbf24', // amber-400
          500: '#f59e0b', // amber-500
          600: '#d97706', // amber-600
        },
        'tov-mystical': {
          400: '#a78bfa', // violet-400
          500: '#8b5cf6', // violet-500
          600: '#7c3aed', // violet-600
        }
      },
      fontFamily: {
        'fantasy': ['Cinzel', 'serif'],
      },
      animation: {
        'fadeInUp': 'fadeInUp 0.3s ease-out forwards',
        'skeleton': 'skeleton 1.5s infinite',
      },
      keyframes: {
        fadeInUp: {
          '0%': { opacity: '0', transform: 'translateY(20px)' },
          '100%': { opacity: '1', transform: 'translateY(0)' },
        },
        skeleton: {
          '0%': { backgroundPosition: '200% 0' },
          '100%': { backgroundPosition: '-200% 0' },
        }
      }
    },
  },
  plugins: [require("daisyui")],
  daisyui: {
    themes: [
      {
        tovDark: {
          "primary": "#8b5cf6",     // violet-500 (мистический пурпурный)
          "primary-focus": "#7c3aed", // violet-600
          "primary-content": "#f1f5f9", // slate-100
          
          "secondary": "#334155",    // slate-700 (темно-синий)
          "secondary-focus": "#1e293b", // slate-800
          "secondary-content": "#fbbf24", // amber-400 (золотой)
          
          "accent": "#fbbf24",       // amber-400 (золотой акцент)
          "accent-focus": "#f59e0b", // amber-500
          "accent-content": "#1e293b", // slate-800
          
          "neutral": "#334155",      // slate-700
          "neutral-focus": "#1e293b", // slate-800
          "neutral-content": "#fbbf24", // amber-400
          
          "base-100": "#0f172a",     // slate-900 (основной фон)
          "base-200": "#1e293b",     // slate-800 (карточки)
          "base-300": "#334155",     // slate-700 (элементы поверх карточек)
          "base-content": "#f1f5f9", // slate-100 (основной текст)
          
          "info": "#3b82f6",         // blue-500
          "info-content": "#dbeafe", // blue-100
          
          "success": "#10b981",      // emerald-500
          "success-content": "#d1fae5", // emerald-100
          
          "warning": "#f59e0b",      // amber-500
          "warning-content": "#fef3c7", // amber-100
          
          "error": "#ef4444",        // red-500
          "error-content": "#fecaca", // red-200
          
          "--rounded-box": "0.5rem",
          "--rounded-btn": "0.25rem",
          "--rounded-badge": "0.125rem",
          "--animation-btn": "0.25s",
          "--animation-input": "0.2s",
          "--btn-focus-scale": "0.95",
          "--border-btn": "1px",
          "--tab-border": "1px",
          "--tab-radius": "0.5rem",
        },
      },
    ],
    darkTheme: "tovDark",
    base: false,
    styled: true,
    utils: true,
    rtl: false,
    prefix: "",
    logs: true,
  },
}