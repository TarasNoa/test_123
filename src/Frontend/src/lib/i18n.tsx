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
  ru: {
    'auth.welcome': 'Добро пожаловать в Libr4',
    'auth.tagline': 'Будущее работы уже здесь.',
    'auth.login': 'Войти',
    'auth.register': 'Зарегистрироваться',
    'auth.email': 'Email',
    'auth.password': 'Пароль',
    'auth.forgotPassword': 'Забыли пароль?',
    'auth.orContinueWith': 'или войти через',
    'auth.noAccount': 'Нет аккаунта?',
    'auth.hasAccount': 'Уже есть аккаунт?',
    'auth.selectRole': 'Выберите вашу роль',
    'auth.role.client': 'Заказчик',
    'auth.role.clientDesc': 'Нанимайте таланты для ваших проектов',
    'auth.role.company': 'Компания',
    'auth.role.companyDesc': 'Управляйте командой и находите подрядчиков',
    'auth.role.freelancer': 'Фрилансер',
    'auth.role.freelancerDesc': 'Находите работу и развивайте карьеру',
    'auth.basicInfo': 'Основная информация',
    'auth.profileDetails': 'Детали профиля',
    'auth.completeRegistration': 'Завершить регистрацию',
    'auth.displayName': 'Имя',
    'auth.phone': 'Телефон',
    'auth.country': 'Страна',
    'auth.city': 'Город',
    'auth.companyName': 'Название компании',
    'auth.industry': 'Отрасль',
    'auth.companySize': 'Размер компании',
    'auth.website': 'Сайт',
    'auth.skills': 'Навыки (через запятую)',
    'auth.experience': 'Стаж (лет)',
    'auth.hourlyRate': 'Почасовая ставка ($)',
    'auth.specialization': 'Специализация',
    'auth.setup2fa': 'Включить двухфакторную аутентификацию',
    'auth.setup2faDesc': 'Защитите аккаунт с помощью приложения-аутентификатора',
    'auth.linkedinUrl': 'Ссылка на LinkedIn',
    'auth.uploadCv': 'Загрузить резюме',
    'auth.uploadCvDesc': 'PDF, DOC, DOCX до 10 МБ',
    'auth.step': 'Шаг',
    'auth.of': 'из',
    'auth.back': 'Назад',
    'auth.next': 'Далее',
    'auth.terms': 'Продолжая, вы соглашаетесь с Условиями использования и Политикой конфиденциальности.',
    'auth.error.generic': 'Что-то пошло не так. Попробуйте ещё раз.',
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
  if (typeof navigator === 'undefined') return 'en';
  const lang = navigator.language?.split('-')[0] || 'en';
  if (lang === 'ru' || lang === 'uk') return 'ru';
  return 'en';
}

export function getRegion() {
  if (typeof navigator === 'undefined') return 'US';
  return navigator.language?.split('-')[1] || 'US';
}

export function getBrowserLocale() {
  if (typeof navigator === 'undefined') return 'en-US';
  const lang = navigator.language || 'en-US';
  if (lang.startsWith('ru') || lang.startsWith('uk')) return 'ru';
  return 'en';
}

export function I18nProvider(props: { children: any }) {
  const [locale, setLocale] = createSignal('en');
  return (
    <I18nContext.Provider value={{ locale, changeLocale: setLocale }}>
      {props.children}
    </I18nContext.Provider>
  );
}
