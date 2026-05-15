import { createSignal, createContext, useContext } from 'solid-js';

const I18nContext = createContext<{ locale: () => string; changeLocale: (l: string) => void }>();

const translations: Record<string, Record<string, string>> = {
  en: {
    'auth.welcome': 'Welcome back',
    'auth.tagline': 'Sign in to continue to Libr4',
    'auth.login': 'Sign In',
    'auth.register': 'Create Account',
    'auth.email': 'Email address',
    'auth.password': 'Password',
    'auth.forgotPassword': 'Forgot password?',
    'auth.orContinueWith': 'or',
    'auth.noAccount': "Don't have an account?",
    'auth.hasAccount': 'Already have an account?',
    'auth.selectRole': 'Select your role',
    'auth.role.client': 'Client',
    'auth.role.clientDesc': 'Post projects and hire talent',
    'auth.role.company': 'Company',
    'auth.role.companyDesc': 'Manage team and projects',
    'auth.role.freelancer': 'Freelancer',
    'auth.role.freelancerDesc': 'Find work and build your career',
    'auth.basicInfo': 'Basic Information',
    'auth.profileDetails': 'Profile Details',
    'auth.completeRegistration': 'Complete Registration',
    'auth.displayName': 'Display name',
    'auth.phone': 'Phone number',
    'auth.country': 'Country',
    'auth.city': 'City',
    'auth.companyName': 'Company name',
    'auth.industry': 'Industry',
    'auth.companySize': 'Company size',
    'auth.website': 'Website',
    'auth.skills': 'Skills',
    'auth.experience': 'Experience',
    'auth.hourlyRate': 'Hourly rate',
    'auth.specialization': 'Specialization',
    'auth.setup2fa': 'Enable two-factor authentication',
    'auth.setup2faDesc': 'Adds an extra layer of security',
    'auth.linkedinUrl': 'LinkedIn profile URL',
    'auth.uploadCv': 'Upload CV',
    'auth.uploadCvDesc': 'PDF, DOC or DOCX',
    'auth.step': 'Step',
    'auth.of': 'of',
    'auth.back': 'Back',
    'auth.next': 'Next',
    'auth.terms': 'By continuing, you agree to our Terms of Service and Privacy Policy.',
    'auth.error.generic': 'Something went wrong. Please try again.',
  },
};

function t(locale: () => string, key: string) {
  return translations[locale()]?.[key] || translations['en']?.[key] || key;
}

export function useI18n() {
  const ctx = useContext(I18nContext);
  if (!ctx) {
    const fallbackLocale = () => 'en';
    return {
      locale: fallbackLocale,
      changeLocale: () => {},
      t: (key: string) => t(fallbackLocale, key),
    };
  }
  return { ...ctx, t: (key: string) => t(ctx.locale, key) };
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
