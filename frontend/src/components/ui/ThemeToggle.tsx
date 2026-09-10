import React from 'react';
import { useTheme } from '../../context/ThemeContext';

/** Minimal sun/moon control for MainLayout top bar (UI-1 only). */
export const ThemeToggle: React.FC = () => {
  const { theme, toggleTheme } = useTheme();
  const isDark = theme === 'dark';

  return (
    <button
      type="button"
      className="theme-toggle"
      onClick={toggleTheme}
      aria-label={isDark ? 'Switch to light theme' : 'Switch to dark theme'}
      title={isDark ? 'Light mode' : 'Dark mode'}
      style={{
        display: 'inline-flex',
        alignItems: 'center',
        justifyContent: 'center',
        width: '2rem',
        height: '2rem',
        padding: 0,
        border: '1px solid var(--border)',
        borderRadius: '0.25rem',
        background: 'var(--surface)',
        color: 'var(--text-primary)',
        cursor: 'pointer',
      }}
    >
      <i className={isDark ? 'bi bi-sun-fill' : 'bi bi-moon-fill'} aria-hidden />
    </button>
  );
};
