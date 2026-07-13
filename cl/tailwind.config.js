/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./src/**/*.{html,ts}",
  ],
  theme: {
    extend: {
      colors: {
        'maritime': {
          'primary': '#1E3A8A',      // Maritime Navy Blue
          'success': '#059669',      // Maritime Success Green
          'warning': '#D97706',      // Maritime Warning Orange
          'error': '#DC2626',        // Maritime Error Red
          'neutral': {
            50: '#F8FAFC',
            100: '#F1F5F9',
            200: '#E2E8F0',
            300: '#CBD5E1',
            400: '#94A3B8',
            500: '#64748B',
            600: '#475569',
            700: '#334155',
            800: '#1E293B',
            900: '#0F172A',
          }
        }
      },
      fontFamily: {
        'inter': ['Inter', 'ui-sans-serif', 'system-ui', 'sans-serif'],
        'mono': ['JetBrains Mono', 'ui-monospace', 'monospace'],
      },
      screens: {
        'xs': '320px',
        'sm': '768px',
        'md': '1024px',
        'lg': '1440px',
        'xl': '1920px',
      },
    },
  },
  plugins: [],
}

