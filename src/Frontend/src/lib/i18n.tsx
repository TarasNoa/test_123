import { createSignal, createContext, useContext } from 'solid-js';

const I18nContext = createContext<{ locale: () => string; changeLocale: (l: string) => void }>();

export function useI18n() {
  const ctx = useContext(I18nContext);
  if (!ctx) {
    return {
      locale: () => 'en',
      changeLocale: () => {},
    };
  }
  return ctx;
}

export function detectLocale() {
  return navigator.language?.split('-')[0] || 'en';
}

export function getRegion() {
  return navigator.language?.split('-')[1] || 'US';
}

export function getBrowserLocale() {
  return navigator.language || 'en-US';
}

export function I18nProvider(props: { children: any }) {
  const [locale, setLocale] = createSignal('en');
  return (
    <I18nContext.Provider value={{ locale, changeLocale: setLocale }}>
      {props.children}
    </I18nContext.Provider>
  );
}
