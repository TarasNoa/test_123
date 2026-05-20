import { createSignal, Show, For, onMount } from 'solid-js';
import { useNavigate } from '@solidjs/router';
import { apiClient, type AuthResponse } from '../../lib/api-client';
import { useI18n, getRegion, getBrowserLocale } from '../../lib/i18n';
import { config } from '../../lib/config';
import type { Component } from 'solid-js';

/* ─── Save full session after login/register ─── */
const saveSession = (response: AuthResponse) => {
  localStorage.setItem('accessToken', response.accessToken);
  localStorage.setItem('refreshToken', response.refreshToken);
  try {
    const payload = JSON.parse(atob(response.accessToken.split('.')[1]));
    localStorage.setItem('userId',      payload.sub       ?? '');
    localStorage.setItem('email',       payload.email     ?? '');
    localStorage.setItem('displayName', payload.display_name ?? payload.displayName ?? '');
    localStorage.setItem('role',        payload.role      ?? '');
  } catch { /* ignore if JWT malformed */ }
};

type UserRole = 'client' | 'company' | 'freelancer';
type RegisterStep = 0 | 1 | 2 | 3;

/* ─── Animated gradient orbs background ─── */
const AuthBackground: Component = () => (
  <div class="fixed inset-0 overflow-hidden -z-10" aria-hidden="true">
    <div class="absolute top-[-10%] left-[-10%] w-[50vw] h-[50vw] rounded-full opacity-20 blur-[120px]" style="background: radial-gradient(circle, #35E0D0 0%, transparent 70%); animation: float1 12s ease-in-out infinite;" />
    <div class="absolute bottom-[-10%] right-[-10%] w-[60vw] h-[60vw] rounded-full opacity-15 blur-[140px]" style="background: radial-gradient(circle, #9B7CFF 0%, transparent 70%); animation: float2 15s ease-in-out infinite;" />
    <div class="absolute top-[40%] left-[60%] w-[30vw] h-[30vw] rounded-full opacity-10 blur-[100px]" style="background: radial-gradient(circle, #35E0D0 0%, transparent 70%); animation: float3 10s ease-in-out infinite;" />
    <div class="absolute inset-0 bg-[#05050a]/80" />
  </div>
);

/* ─── OAuth Provider SVG icons ─── */
const ProviderIcons: Record<string, () => any> = {
  google: () => (
    <svg class="w-5 h-5" viewBox="0 0 24 24" fill="currentColor">
      <path d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z" fill="#4285F4"/>
      <path d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z" fill="#34A853"/>
      <path d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z" fill="#FBBC05"/>
      <path d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z" fill="#EA4335"/>
    </svg>
  ),
  github: () => (
    <svg class="w-5 h-5" viewBox="0 0 24 24" fill="currentColor">
      <path d="M12 2C6.477 2 2 6.484 2 12.017c0 4.425 2.865 8.18 6.839 9.504.5.092.682-.217.682-.483 0-.237-.008-.868-.013-1.703-2.782.605-3.369-1.343-3.369-1.343-.454-1.158-1.11-1.466-1.11-1.466-.908-.62.069-.608.069-.608 1.003.07 1.531 1.032 1.531 1.032.892 1.53 2.341 1.088 2.91.832.092-.647.35-1.088.636-1.338-2.22-.253-4.555-1.113-4.555-4.951 0-1.093.39-1.988 1.029-2.688-.103-.253-.446-1.272.098-2.65 0 0 .84-.27 2.75 1.026A9.564 9.564 0 0112 6.844c.85.004 1.705.115 2.504.337 1.909-1.296 2.747-1.027 2.747-1.027.546 1.379.202 2.398.1 2.651.64.7 1.028 1.595 1.028 2.688 0 3.848-2.339 4.695-4.566 4.943.359.309.678.92.678 1.855 0 1.338-.012 2.419-.012 2.747 0 .268.18.58.688.482A10.019 10.019 0 0022 12.017C22 6.484 17.522 2 12 2z"/>
    </svg>
  ),
  facebook: () => (
    <svg class="w-5 h-5" viewBox="0 0 24 24" fill="currentColor">
      <path d="M24 12.073c0-6.627-5.373-12-12-12s-12 5.373-12 12c0 5.99 4.388 10.954 10.125 11.854v-8.385H7.078v-3.47h3.047V9.43c0-3.007 1.792-4.669 4.533-4.669 1.312 0 2.686.235 2.686.235v2.953H15.83c-1.491 0-1.956.925-1.956 1.874v2.25h3.328l-.532 3.47h-2.796v8.385C19.612 23.027 24 18.062 24 12.073z"/>
    </svg>
  ),
  apple: () => (
    <svg class="w-5 h-5" viewBox="0 0 24 24" fill="currentColor">
      <path d="M17.05 20.28c-.98.95-2.05.88-3.08.4-1.09-.5-2.09-.48-3.18 0-1.38.62-2.46.45-3.28-.4C4.24 16.73 3.03 11.59 6.04 8.64c1.49-1.43 3.43-1.62 4.92-.4 1.1.9 2.07.9 3.28 0 1.71-1.31 4.39-.97 5.56.66-4.95 2.92-4.12 10.13.15 13.38zM12.03 7.25c-.15-2.23 1.66-4.07 3.74-4.25.29 2.58-2.34 4.5-3.74 4.25z"/>
    </svg>
  ),
  microsoft: () => (
    <svg class="w-5 h-5" viewBox="0 0 24 24" fill="none">
      <path d="M11 11H0V0h11v11z" fill="#f25022"/>
      <path d="M24 11H13V0h11v11z" fill="#7fba00"/>
      <path d="M11 24H0V13h11v11z" fill="#00a4ef"/>
      <path d="M24 24H13V13h11v11z" fill="#ffb900"/>
    </svg>
  ),
  linkedin: () => (
    <svg class="w-5 h-5" viewBox="0 0 24 24" fill="currentColor">
      <path d="M20.447 20.452h-3.554v-5.569c0-1.328-.027-3.037-1.852-3.037-1.853 0-2.136 1.445-2.136 2.939v5.667H9.351V9h3.414v1.561h.046c.477-.9 1.637-1.85 3.37-1.85 3.601 0 4.267 2.37 4.267 5.455v6.286zM5.337 7.433a2.062 2.062 0 01-2.063-2.065 2.064 2.064 0 112.063 2.065zm1.782 13.019H3.555V9h3.564v11.452zM22.225 0H1.771C.792 0 0 .774 0 1.729v20.542C0 23.227.792 24 1.771 24h20.451C23.2 24 24 23.227 24 22.271V1.729C24 .774 23.2 0 22.222 0h.003z"/>
    </svg>
  ),
  discord: () => (
    <svg class="w-5 h-5" viewBox="0 0 24 24" fill="currentColor">
      <path d="M20.317 4.37a19.791 19.791 0 00-4.885-1.515.074.074 0 00-.079.037c-.21.375-.444.864-.608 1.25a18.27 18.27 0 00-5.487 0 12.64 12.64 0 00-.617-1.25.077.077 0 00-.079-.037A19.736 19.736 0 003.677 4.37a.07.07 0 00-.032.027C.533 9.046-.32 13.58.099 18.057a.082.082 0 00.031.057 19.9 19.9 0 005.993 3.03.078.078 0 00.084-.028c.462-.63.874-1.295 1.226-1.994a.076.076 0 00-.041-.106 13.107 13.107 0 01-1.872-.892.077.077 0 01-.008-.128 10.2 10.2 0 00.372-.292.074.074 0 01.077-.01c3.928 1.793 8.18 1.793 12.062 0a.074.074 0 01.078.01c.12.098.246.198.373.292a.077.077 0 01-.006.127 12.299 12.299 0 01-1.873.892.077.077 0 00-.041.107c.36.699.772 1.362 1.225 1.993a.076.076 0 00.084.028 19.839 19.839 0 006.002-3.03.077.077 0 00.032-.054c.5-5.177-.838-9.674-3.549-13.66a.061.061 0 00-.031-.03zM8.02 15.33c-1.183 0-2.157-1.085-2.157-2.419 0-1.333.956-2.419 2.157-2.419 1.21 0 2.176 1.096 2.157 2.42 0 1.333-.956 2.418-2.157 2.418zm7.975 0c-1.183 0-2.157-1.085-2.157-2.419 0-1.333.955-2.419 2.157-2.419 1.21 0 2.176 1.096 2.157 2.42 0 1.333-.946 2.418-2.157 2.418z"/>
    </svg>
  ),
  twitter: () => (
    <svg class="w-5 h-5" viewBox="0 0 24 24" fill="currentColor">
      <path d="M18.244 2.25h3.308l-7.227 8.26 8.502 11.24H16.17l-5.214-6.817L4.99 21.75H1.68l7.73-8.835L1.254 2.25H8.08l4.713 6.231zm-1.161 17.52h1.833L7.084 4.126H5.117z"/>
    </svg>
  ),
  vk: () => (
    <svg class="w-5 h-5" viewBox="0 0 24 24" fill="currentColor">
      <path d="M12.785 16.241s.288-.032.437-.194c.136-.148.132-.427.132-.427s-.02-1.304.587-1.496c.596-.188 1.36 1.26 2.172 1.817.614.422 1.082.33 1.082.33l2.167-.03s1.135-.07.597-.965c-.044-.073-.314-.66-1.617-1.868-1.364-1.264-1.183-1.06.462-3.246.998-1.332 1.397-2.146 1.273-2.493-.118-.33-.847-.243-.847-.243l-2.44.015s-.18-.025-.314.056c-.13.078-.215.26-.215.26s-.387 1.03-.903 1.904c-1.083 1.84-1.518 1.937-1.694 1.82-.413-.252-.31-1.014-.31-1.553 0-1.688.256-2.393-.498-2.578-.25-.064-.434-.106-1.072-.113-.818-.01-1.51.003-1.902.194-.26.126-.46.405-.338.42.152.02.495.093.676.34.235.32.226 1.04.226 1.04s.134 1.002-.313 1.127c-.306.095-.816-.098-1.812-1.017-.513-.472-.904-1.054-1.19-1.48-.358-.537-.51-.994-.587-1.145-.053-.105-.12-.146-.203-.15L5.86 7.04s-.38.01-.52.175c-.123.144-.01.443-.01.443s1.496 3.493 3.17 5.25c1.546 1.623 3.302 1.49 3.302 1.49h.796z"/>
    </svg>
  ),
  wechat: () => (
    <svg class="w-5 h-5" viewBox="0 0 24 24" fill="currentColor">
      <path d="M8.691 2.188C3.891 2.188 0 5.476 0 9.53c0 2.212 1.17 4.203 3.002 5.55a.59.59 0 01.213.665l-.39 1.48c-.019.07-.048.141-.048.213 0 .163.13.295.29.295a.326.326 0 00.167-.054l1.903-1.114a.864.864 0 01.717-.098 10.16 10.16 0 002.837.403c.276 0 .543-.027.811-.05-.857-2.578.157-4.972 1.932-6.446 1.703-1.415 3.882-1.98 5.853-1.838-.576-3.583-4.196-6.348-8.596-6.348zM5.785 5.991c.642 0 1.162.529 1.162 1.18a1.17 1.17 0 01-1.162 1.178A1.17 1.17 0 014.623 7.17c0-.651.52-1.18 1.162-1.18zm5.813 0c.642 0 1.162.529 1.162 1.18a1.17 1.17 0 01-1.162 1.178 1.17 1.17 0 01-1.162-1.178c0-.651.52-1.18 1.162-1.18zm5.34 2.867c-1.797-.052-3.746.512-5.28 1.786-1.72 1.428-2.687 3.72-1.78 6.22.942 2.453 3.666 4.229 6.884 4.229.826 0 1.622-.12 2.361-.336a.722.722 0 01.598.082l1.584.926a.272.272 0 00.14.045c.134 0 .24-.111.24-.247 0-.06-.023-.12-.038-.177l-.327-1.233a.582.582 0 01-.023-.156.49.49 0 01.201-.398C23.024 18.48 24 16.82 24 14.98c0-3.21-2.931-5.837-7.062-6.122zM14.51 13.88a.94.94 0 01.939-.943.94.94 0 01.94.943.94.94 0 01-.94.943.94.94 0 01-.939-.943zm4.963 0a.94.94 0 01.94-.943.94.94 0 01.939.943.94.94 0 01-.94.943.94.94 0 01-.939-.943z"/>
    </svg>
  ),
  telegram: () => (
    <svg class="w-5 h-5" viewBox="0 0 24 24" fill="currentColor">
      <path d="M11.944 0A12 12 0 0 0 0 12a12 12 0 0 0 12 12 12 12 0 0 0 12-12A12 12 0 0 0 12 0a12 12 0 0 0-.056 0zm4.962 7.224c.1-.002.321.023.465.14a.506.506 0 0 1 .171.325c.016.093.036.306.02.472-.18 1.898-.962 6.502-1.36 8.627-.168.9-.499 1.201-.82 1.23-.696.065-1.225-.46-1.9-.902-1.056-.693-1.653-1.124-2.678-1.8-1.185-.78-.417-1.21.258-1.91.177-.184 3.247-2.977 3.307-3.23.007-.032.014-.15-.056-.212s-.174-.041-.249-.024c-.106.024-1.793 1.14-5.061 3.345-.479.33-.913.49-1.3.48-.428-.008-1.252-.241-1.865-.44-.752-.245-1.349-.374-1.297-.789.027-.216.325-.437.893-.663 3.498-1.524 5.83-2.529 6.998-3.014 3.332-1.386 4.025-1.627 4.477-1.635z"/>
    </svg>
  ),
  yandex: () => (
    <svg class="w-5 h-5" viewBox="0 0 24 24" fill="currentColor">
      <path d="M10.95 9.84L7.36 18h2.66l.8-2.18h3.83l-.76 2.18h2.66L12.05 9.84h-1.1zm1.18 1.18l1.42 3.88h-2.84l1.42-3.88z"/>
    </svg>
  ),
  reddit: () => (
    <svg class="w-5 h-5" viewBox="0 0 24 24" fill="currentColor">
      <path d="M12 0A12 12 0 0 0 0 12a12 12 0 0 0 12 12 12 12 0 0 0 12-12A12 12 0 0 0 12 0zm5.01 4.744c.688 0 1.25.561 1.25 1.249a1.25 1.25 0 0 1-2.498.056l-2.597-.547-.8 3.747c1.824.07 3.48.632 4.674 1.488.308-.309.73-.491 1.207-.491.968 0 1.754.786 1.754 1.754 0 .716-.435 1.333-1.01 1.614a3.111 3.111 0 0 1 .042.52c0 2.694-3.13 4.87-6.99 4.87-3.86 0-6.99-2.176-6.99-4.87 0-.183.015-.366.043-.534A1.748 1.748 0 0 1 4.028 12c0-.968.786-1.754 1.754-1.754.463 0 .898.196 1.207.49 1.207-.883 2.878-1.43 4.744-1.53l.358-1.68c.07-.325.37-.555.704-.555.053 0 .105.006.156.018l2.596.547c.134.028.255.1.341.204z"/>
    </svg>
  ),
};

const allProviders = [
  { key: 'google', name: 'Google', global: true },
  { key: 'apple', name: 'Apple', global: true },
  { key: 'microsoft', name: 'Microsoft', global: true },
  { key: 'github', name: 'GitHub', global: true },
  { key: 'discord', name: 'Discord', global: true },
  { key: 'twitter', name: 'X', global: true },
  { key: 'vk', name: 'VK', regions: ['RU', 'BY', 'KZ', 'UA'] },
  { key: 'yandex', name: 'Yandex', regions: ['RU', 'BY', 'KZ', 'UA'] },
  { key: 'wechat', name: 'WeChat', regions: ['CN', 'HK', 'MO', 'TW'] },
  { key: 'qq', name: 'QQ', regions: ['CN', 'HK', 'MO', 'TW'] },
  { key: 'line', name: 'LINE', regions: ['JP'] },
  { key: 'kakaotalk', name: 'Kakao', regions: ['KR'] },
  { key: 'naver', name: 'Naver', regions: ['KR'] },
  { key: 'zalo', name: 'Zalo', regions: ['VN'] },
];

const ClientIcon: Component = () => (
  <svg class="w-5 h-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round">
    <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"/><circle cx="12" cy="7" r="4"/>
  </svg>
);
const CompanyIcon: Component = () => (
  <svg class="w-5 h-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round">
    <path d="M3 22V12a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2v10"/><path d="M12 2 2 10h20L12 2z"/><path d="M9 22v-5h6v5"/>
  </svg>
);
const FreelancerIcon: Component = () => (
  <svg class="w-5 h-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round">
    <rect x="2" y="7" width="20" height="14" rx="2"/><path d="M16 21V5a2 2 0 0 0-2-2h-4a2 2 0 0 0-2 2v16"/>
  </svg>
);

const roles: { key: UserRole; Icon: Component; title: string; desc: string }[] = [
  { key: 'client', Icon: ClientIcon, title: 'auth.role.client', desc: 'auth.role.clientDesc' },
  { key: 'company', Icon: CompanyIcon, title: 'auth.role.company', desc: 'auth.role.companyDesc' },
  { key: 'freelancer', Icon: FreelancerIcon, title: 'auth.role.freelancer', desc: 'auth.role.freelancerDesc' },
];

export default function Auth() {
  const { t, changeLocale } = useI18n();
  const navigate = useNavigate();
  const [region, setRegion] = createSignal('US');
  const [isLogin, setIsLogin] = createSignal(true);
  const [regStep, setRegStep] = createSignal<RegisterStep>(0);
  const [role, setRole] = createSignal<UserRole | null>(null);
  const [error, setError] = createSignal('');
  const [loading, setLoading] = createSignal(false);

  /* Basic info */
  const [email, setEmail] = createSignal('');
  const [displayName, setDisplayName] = createSignal('');
  const [password, setPassword] = createSignal('');
  const [phone, setPhone] = createSignal('');
  const [country, setCountry] = createSignal('');
  const [city, setCity] = createSignal('');

  /* Role-specific */
  const [companyName, setCompanyName] = createSignal('');
  const [industry, setIndustry] = createSignal('');
  const [companySize, setCompanySize] = createSignal('');
  const [website, setWebsite] = createSignal('');
  const [skills, setSkills] = createSignal('');
  const [experience, setExperience] = createSignal('');
  const [hourlyRate, setHourlyRate] = createSignal('');
  const [specialization, setSpecialization] = createSignal('');

  /* Final step */
  const [linkedInUrl, setLinkedInUrl] = createSignal('');
  const [cvFile, setCvFile] = createSignal<File | null>(null);
  const [enable2fa, setEnable2fa] = createSignal(false);

  /* Validation & forgot password */
  const [emailError, setEmailError] = createSignal('');
  const [passwordError, setPasswordError] = createSignal('');
  const [passwordStrength, setPasswordStrength] = createSignal(0);
  const [forgotMode, setForgotMode] = createSignal(false);
  const [resetSent, setResetSent] = createSignal(false);

  onMount(() => {
    setRegion(getRegion());
    changeLocale(getBrowserLocale());
  });

  const filteredProviders = () => allProviders.filter(p => p.global || (p.regions && p.regions.includes(region())));

  const handleOAuth = (provider: string) => {
    window.location.href = `/api/v1/auth/external/${provider}/challenge`;
  };

  /* ─── Validation helpers ─── */
  const validateEmail = (val: string) => {
    const re = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    setEmailError(re.test(val) ? '' : 'Enter a valid email address');
  };

  const validatePassword = (val: string) => {
    if (val.length < 8) {
      setPasswordError('At least 8 characters required');
      setPasswordStrength(0);
      return;
    }
    setPasswordError('');
    let score = 0;
    if (/[a-z]/.test(val)) score++;
    if (/[A-Z]/.test(val)) score++;
    if (/[0-9]/.test(val)) score++;
    if (/[^a-zA-Z0-9]/.test(val)) score++;
    setPasswordStrength(score);
  };

  const handleLogin = async (e: Event) => {
    e.preventDefault();
    setError('');
    validateEmail(email());
    if (emailError()) return;
    setLoading(true);
    try {
      const response = await apiClient.login({ email: email(), password: password() });
      saveSession(response);
      navigate('/dashboard');
    } catch (err: any) {
      setError(err.message || t('auth.error.generic'));
    } finally {
      setLoading(false);
    }
  };

  const handleRegister = async (e: Event) => {
    e.preventDefault();
    setError('');
    validateEmail(email());
    validatePassword(password());
    if (emailError() || passwordError()) return;
    setLoading(true);
    try {
      const r = role()!;
      const response = await apiClient.register({
        email: email(),
        displayName: displayName(),
        password: password(),
        role: r,
        phone: phone() || undefined,
        country: country() || undefined,
        city: city() || undefined,
        companyName: companyName() || undefined,
        industry: industry() || undefined,
        companySize: companySize() || undefined,
        website: website() || undefined,
        skills: skills() || undefined,
        experience: experience() || undefined,
        hourlyRate: hourlyRate() || undefined,
        specialization: specialization() || undefined,
        linkedInUrl: linkedInUrl() || undefined,
      });
      saveSession(response);
      if (cvFile()) {
        try { await apiClient.uploadCv(cvFile()!); } catch {}
      }
      navigate('/dashboard');
    } catch (err: any) {
      setError(err.message || t('auth.error.generic'));
    } finally {
      setLoading(false);
    }
  };

  const handleForgotRequest = async (e: Event) => {
    e.preventDefault();
    setLoading(true); setError('');
    try {
      await fetch(`${config.apiBaseUrl}/api/v1/auth/password/reset-request`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email: email() }),
      });
      setResetSent(true);
    } catch {
      setResetSent(true);
    } finally {
      setLoading(false);
    }
  };

  const nextStep = () => setRegStep((s) => (s < 3 ? ((s + 1) as RegisterStep) : s));
  const prevStep = () => setRegStep((s) => (s > 0 ? ((s - 1) as RegisterStep) : s));

  const inputCls = "w-full px-4 py-3 bg-surface-2/60 border border-surface-3/60 rounded-xl text-foreground text-sm placeholder:text-muted-foreground/40 focus:outline-none focus:ring-2 focus:ring-secondary/30 focus:border-secondary/50 transition-all";

  return (
    <div class="relative min-h-screen flex items-center justify-center overflow-hidden text-foreground">
      <AuthBackground />
      <style>{`
        @keyframes float1 { 0%,100%{transform:translate(0,0)} 50%{transform:translate(30px,-20px)} }
        @keyframes float2 { 0%,100%{transform:translate(0,0)} 50%{transform:translate(-20px,30px)} }
        @keyframes float3 { 0%,100%{transform:translate(0,0)} 50%{transform:translate(20px,20px)} }
        @keyframes fadeUp { from{opacity:0;transform:translateY(20px)} to{opacity:1;transform:translateY(0)} }
      `}</style>

      <div class="w-full max-w-[420px] px-4 sm:px-0" style="animation: fadeUp 0.6s ease-out">
        <div class="text-center mb-8">
          <div class="inline-flex items-center justify-center w-14 h-14 rounded-2xl bg-gradient-to-br from-[#35E0D0] to-[#9B7CFF] mb-4 shadow-lg shadow-primary/20">
            <span class="text-2xl font-black text-black">L4</span>
          </div>
          <h1 class="text-3xl font-bold tracking-tight mb-2 bg-gradient-to-r from-white via-[#35E0D0] to-[#9B7CFF] bg-clip-text text-transparent">{t('auth.welcome')}</h1>
          <p class="text-sm text-muted-foreground leading-relaxed">{t('auth.tagline')}</p>
        </div>

        <div class="relative rounded-3xl border border-white/10 bg-white/[0.03] backdrop-blur-2xl shadow-2xl shadow-black/40 overflow-hidden min-h-[420px] sm:min-h-[520px] flex flex-col">
          <div class="absolute top-0 left-0 right-0 h-px bg-gradient-to-r from-transparent via-[#35E0D0]/50 to-transparent" />
          <div class="p-7 space-y-5 flex-1 flex flex-col">
            {/* Tabs */}
            <Show when={regStep() === 0}>
              <div class="flex p-1 rounded-xl bg-surface-2/50 border border-surface-3/50">
                <button type="button" onClick={() => { setIsLogin(true); setError(''); }} class={isLogin() ? 'flex-1 py-2 text-sm font-semibold rounded-lg bg-primary/10 text-primary transition-all' : 'flex-1 py-2 text-sm font-medium rounded-lg text-muted-foreground hover:text-foreground transition-all'}>{t('auth.login')}</button>
                <button type="button" onClick={() => { setIsLogin(false); setError(''); setRegStep(0); }} class={!isLogin() ? 'flex-1 py-2 text-sm font-semibold rounded-lg bg-secondary/10 text-secondary transition-all' : 'flex-1 py-2 text-sm font-medium rounded-lg text-muted-foreground hover:text-foreground transition-all'}>{t('auth.register')}</button>
              </div>
            </Show>

            {/* Wizard step indicator */}
            <Show when={!isLogin() && regStep() > 0}>
              <div class="flex items-center justify-between text-xs text-muted-foreground">
                <span>{t('auth.step')} {regStep()} {t('auth.of')} 3</span>
                <button type="button" onClick={() => { setRegStep(0); setRole(null); }} class="hover:text-primary transition-colors">{t('auth.back')}</button>
              </div>
              <div class="h-1 bg-surface-2 rounded-full overflow-hidden">
                <div class="h-full bg-gradient-to-r from-[#35E0D0] to-[#9B7CFF] transition-all" style={{ width: `${(regStep() / 3) * 100}%` }} />
              </div>
            </Show>

            <Show when={error()}>
              <div class="flex items-center gap-2 p-3 rounded-xl bg-red-500/10 border border-red-500/20 text-red-400 text-sm">
                <svg class="w-4 h-4 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>
                {error()}
              </div>
            </Show>

            {/* ─── LOGIN FORM ─── */}
            <Show when={isLogin()}>
              <Show when={!forgotMode()}>
                <form onSubmit={handleLogin} class="space-y-4">
                  <div class="space-y-3">
                    <div class="space-y-1">
                      <input type="email" value={email()} onInput={e => { setEmail(e.currentTarget.value); validateEmail(e.currentTarget.value); }} required placeholder={t('auth.email')} class={`${inputCls} ${emailError() ? 'border-red-500/50 focus:border-red-500/50 focus:ring-red-500/20' : ''}`} />
                      <Show when={emailError()}>
                        <p class="text-xs text-red-400 px-1">{emailError()}</p>
                      </Show>
                    </div>
                    <input type="password" value={password()} onInput={e => setPassword(e.currentTarget.value)} required placeholder={t('auth.password')} class={inputCls} />
                  </div>
                  <div class="flex justify-end">
                    <button type="button" onClick={() => { setForgotMode(true); setError(''); setEmailError(''); }} class="text-xs text-muted-foreground hover:text-primary transition-colors">{t('auth.forgotPassword')}</button>
                  </div>
                  <button type="submit" disabled={loading()} class="w-full py-3 bg-gradient-to-r from-[#35E0D0] to-[#2bc4b6] text-black font-bold rounded-xl hover:opacity-90 active:scale-[0.98] disabled:opacity-50 transition-all shadow-lg shadow-primary/20">{loading() ? 'Loading...' : t('auth.login')}</button>
                </form>

                <div class="relative flex items-center gap-3">
                  <div class="flex-1 h-px bg-gradient-to-r from-transparent via-surface-3 to-transparent" />
                  <span class="text-[11px] uppercase tracking-widest text-muted-foreground/60 font-medium shrink-0">{t('auth.orContinueWith')}</span>
                  <div class="flex-1 h-px bg-gradient-to-r from-transparent via-surface-3 to-transparent" />
                </div>

                <div class="grid grid-cols-2 sm:grid-cols-4 gap-2.5">
                  <For each={filteredProviders()}>{(p) => {
                    const Icon = ProviderIcons[p.key];
                    return (
                      <button type="button" onClick={() => handleOAuth(p.key)} class="group flex flex-col items-center gap-1.5 p-2.5 rounded-xl bg-surface-2/40 border border-surface-3/40 hover:bg-surface-2/80 hover:border-secondary/30 hover:scale-105 transition-all" title={p.name}>
                        <div class="text-muted-foreground group-hover:text-foreground transition-colors">{Icon ? <Icon /> : <span class="text-xs">{p.name[0]}</span>}</div>
                        <span class="text-[10px] text-muted-foreground/70 group-hover:text-foreground/90 transition-colors truncate w-full text-center">{p.name}</span>
                      </button>
                    );
                  }}</For>
                </div>
              </Show>

              {/* ─── FORGOT PASSWORD ─── */}
              <Show when={forgotMode() && !resetSent()}>
                <form onSubmit={handleForgotRequest} class="space-y-4">
                  <h2 class="text-base font-semibold text-center">Reset password</h2>
                  <p class="text-xs text-muted-foreground text-center">Enter your email and we'll send a reset link.</p>
                  <input type="email" value={email()} onInput={e => setEmail(e.currentTarget.value)} required placeholder={t('auth.email')} class={inputCls} />
                  <div class="flex gap-3">
                    <button type="button" onClick={() => { setForgotMode(false); setResetSent(false); }} class="flex-1 py-2.5 rounded-xl bg-surface-2/60 text-sm hover:bg-surface-2/80 transition-all">Back</button>
                    <button type="submit" disabled={loading()} class="flex-1 py-2.5 bg-gradient-to-r from-[#35E0D0] to-[#2bc4b6] text-black text-sm font-bold rounded-xl hover:opacity-90 disabled:opacity-50 transition-all">{loading() ? 'Sending…' : 'Send reset link'}</button>
                  </div>
                </form>
              </Show>

              <Show when={forgotMode() && resetSent()}>
                <div class="text-center space-y-4 py-4">
                  <div class="w-12 h-12 rounded-full bg-success/10 border border-success/20 flex items-center justify-center mx-auto">
                    <svg class="w-6 h-6 text-success" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M5 13l4 4L19 7" /></svg>
                  </div>
                  <p class="text-sm font-medium">Check your email</p>
                  <p class="text-xs text-muted-foreground">We sent a reset link to {email()}</p>
                  <button type="button" onClick={() => { setForgotMode(false); setResetSent(false); }} class="text-sm text-secondary hover:underline">Back to login</button>
                </div>
              </Show>
            </Show>

            {/* ─── REGISTRATION WIZARD ─── */}
            <Show when={!isLogin()}>
              {/* Step 0: Role selection */}
              <Show when={regStep() === 0}>
                <div class="space-y-3">
                  <h2 class="text-lg font-semibold text-center">{t('auth.selectRole')}</h2>
                  <div class="grid gap-3">
                    <For each={roles}>{(r) => (
                      <button type="button" onClick={() => { setRole(r.key); setRegStep(1); setError(''); }} class="flex items-center gap-4 p-4 rounded-xl bg-surface-2/40 border border-surface-3/40 hover:bg-surface-2/80 hover:border-secondary/30 hover:scale-[1.02] transition-all text-left">
                        <div class="w-10 h-10 rounded-lg bg-secondary/10 text-secondary flex items-center justify-center shrink-0"><r.Icon /></div>
                        <div>
                          <div class="font-semibold text-sm">{t(r.title)}</div>
                          <div class="text-xs text-muted-foreground">{t(r.desc)}</div>
                        </div>
                      </button>
                    )}</For>
                  </div>
                </div>
              </Show>

              {/* Step 1: Basic info */}
              <Show when={regStep() === 1}>
                <form onSubmit={(e) => { e.preventDefault(); validateEmail(email()); validatePassword(password()); if (!emailError() && !passwordError()) nextStep(); }} class="space-y-4">
                  <h2 class="text-lg font-semibold text-center">{t('auth.basicInfo')}</h2>
                  <div class="space-y-3">
                    <div class="space-y-1">
                      <input type="email" value={email()} onInput={e => { setEmail(e.currentTarget.value); validateEmail(e.currentTarget.value); }} required placeholder={t('auth.email')} class={`${inputCls} ${emailError() ? 'border-red-500/50 focus:border-red-500/50 focus:ring-red-500/20' : ''}`} />
                      <Show when={emailError()}>
                        <p class="text-xs text-red-400 px-1">{emailError()}</p>
                      </Show>
                    </div>
                    <input type="text" value={displayName()} onInput={e => setDisplayName(e.currentTarget.value)} required placeholder={t('auth.displayName')} class={inputCls} />
                    <div class="space-y-1">
                      <input type="password" value={password()} onInput={e => { setPassword(e.currentTarget.value); validatePassword(e.currentTarget.value); }} required minLength={8} placeholder={t('auth.password')} class={`${inputCls} ${passwordError() ? 'border-red-500/50 focus:border-red-500/50 focus:ring-red-500/20' : ''}`} />
                      <Show when={passwordError()}>
                        <p class="text-xs text-red-400 px-1">{passwordError()}</p>
                      </Show>
                    </div>
                    <Show when={password().length > 0}>
                      <div class="space-y-1">
                        <div class="flex gap-1">
                          {[1,2,3,4].map(i => (
                            <div class={`h-1 flex-1 rounded-full transition-all ${
                              i <= passwordStrength()
                                ? passwordStrength() <= 1 ? 'bg-red-500'
                                : passwordStrength() <= 2 ? 'bg-yellow-500'
                                : passwordStrength() <= 3 ? 'bg-blue-500'
                                : 'bg-green-500'
                                : 'bg-surface-3'
                            }`} />
                          ))}
                        </div>
                        <p class="text-[10px] text-muted-foreground">
                          {['', 'Weak', 'Fair', 'Good', 'Strong'][passwordStrength()]}
                        </p>
                      </div>
                    </Show>
                    <input type="tel" value={phone()} onInput={e => setPhone(e.currentTarget.value)} placeholder={t('auth.phone')} class={inputCls} />
                    <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
                      <input type="text" value={country()} onInput={e => setCountry(e.currentTarget.value)} placeholder={t('auth.country')} class={inputCls} />
                      <input type="text" value={city()} onInput={e => setCity(e.currentTarget.value)} placeholder={t('auth.city')} class={inputCls} />
                    </div>
                  </div>
                  <div class="flex gap-3">
                    <button type="button" onClick={prevStep} class="flex-1 py-3 rounded-xl bg-surface-2/60 text-sm font-medium hover:bg-surface-2/80 transition-all">{t('auth.back')}</button>
                    <button type="submit" class="flex-1 py-3 bg-gradient-to-r from-[#35E0D0] to-[#2bc4b6] text-black font-bold rounded-xl hover:opacity-90 transition-all shadow-lg shadow-primary/20">{t('auth.next')}</button>
                  </div>
                </form>
              </Show>

              {/* Step 2: Profile details */}
              <Show when={regStep() === 2}>
                <form onSubmit={(e) => { e.preventDefault(); nextStep(); }} class="space-y-4">
                  <h2 class="text-lg font-semibold text-center">{t('auth.profileDetails')}</h2>
                  <div class="space-y-3">
                    <Show when={role() === 'client' || role() === 'company'}>
                      <input type="text" value={companyName()} onInput={e => setCompanyName(e.currentTarget.value)} required placeholder={t('auth.companyName')} class={inputCls} />
                      <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
                        <input type="text" value={industry()} onInput={e => setIndustry(e.currentTarget.value)} placeholder={t('auth.industry')} class={inputCls} />
                        <input type="text" value={companySize()} onInput={e => setCompanySize(e.currentTarget.value)} placeholder={t('auth.companySize')} class={inputCls} />
                      </div>
                    </Show>
                    <Show when={role() === 'company'}>
                      <input type="url" value={website()} onInput={e => setWebsite(e.currentTarget.value)} placeholder={t('auth.website')} class={inputCls} />
                    </Show>
                    <Show when={role() === 'freelancer'}>
                      <input type="text" value={skills()} onInput={e => setSkills(e.currentTarget.value)} placeholder={t('auth.skills')} class={inputCls} />
                      <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
                        <input type="text" value={experience()} onInput={e => setExperience(e.currentTarget.value)} placeholder={t('auth.experience')} class={inputCls} />
                        <input type="text" value={hourlyRate()} onInput={e => setHourlyRate(e.currentTarget.value)} placeholder={t('auth.hourlyRate')} class={inputCls} />
                      </div>
                      <input type="text" value={specialization()} onInput={e => setSpecialization(e.currentTarget.value)} placeholder={t('auth.specialization')} class={inputCls} />
                    </Show>
                  </div>
                  <div class="flex gap-3">
                    <button type="button" onClick={prevStep} class="flex-1 py-3 rounded-xl bg-surface-2/60 text-sm font-medium hover:bg-surface-2/80 transition-all">{t('auth.back')}</button>
                    <button type="submit" class="flex-1 py-3 bg-gradient-to-r from-[#35E0D0] to-[#2bc4b6] text-black font-bold rounded-xl hover:opacity-90 transition-all shadow-lg shadow-primary/20">{t('auth.next')}</button>
                  </div>
                </form>
              </Show>

              {/* Step 3: 2FA + LinkedIn + CV */}
              <Show when={regStep() === 3}>
                <form onSubmit={handleRegister} class="space-y-4">
                  <h2 class="text-lg font-semibold text-center">{t('auth.completeRegistration')}</h2>

                  {/* 2FA */}
                  <div class="p-4 rounded-xl bg-surface-2/40 border border-surface-3/40 space-y-2">
                    <div class="flex items-center gap-3">
                      <input id="2fa" type="checkbox" checked={enable2fa()} onChange={e => setEnable2fa(e.currentTarget.checked)} class="w-4 h-4 accent-primary rounded" />
                      <label for="2fa" class="text-sm font-medium cursor-pointer">{t('auth.setup2fa')}</label>
                    </div>
                    <p class="text-xs text-muted-foreground pl-7">{t('auth.setup2faDesc')}</p>
                  </div>

                  {/* LinkedIn */}
                  <input type="url" value={linkedInUrl()} onInput={e => setLinkedInUrl(e.currentTarget.value)} placeholder={t('auth.linkedinUrl')} class={inputCls} />

                  {/* CV Upload */}
                  <div class="space-y-2">
                    <label class="block text-sm font-medium">{t('auth.uploadCv')}</label>
                    <label class="flex flex-col items-center justify-center gap-2 p-4 rounded-xl border border-dashed border-surface-3/60 bg-surface-2/20 cursor-pointer hover:bg-surface-2/40 transition-all">
                      <svg class="w-6 h-6 text-muted-foreground" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.5"><path stroke-linecap="round" stroke-linejoin="round" d="M12 16.5V4.5m0 0l-4 4m4-4l4 4M3 15.75v1.5a2.25 2.25 0 002.25 2.25h13.5A2.25 2.25 0 0021 17.25v-1.5" /></svg>
                      <span class="text-xs text-muted-foreground">{cvFile() ? cvFile()!.name : t('auth.uploadCvDesc')}</span>
                      <input type="file" accept=".pdf,.doc,.docx" class="hidden" onChange={e => { const f = e.currentTarget.files?.[0]; if (f) setCvFile(f); }} />
                    </label>
                  </div>

                  <div class="flex gap-3">
                    <button type="button" onClick={prevStep} class="flex-1 py-3 rounded-xl bg-surface-2/60 text-sm font-medium hover:bg-surface-2/80 transition-all">{t('auth.back')}</button>
                    <button type="submit" disabled={loading()} class="flex-1 py-3 bg-gradient-to-r from-[#35E0D0] to-[#2bc4b6] text-black font-bold rounded-xl hover:opacity-90 active:scale-[0.98] disabled:opacity-50 transition-all shadow-lg shadow-primary/20">
                      <Show when={loading()} fallback={t('auth.completeRegistration')}>
                        <span class="inline-flex items-center gap-2">
                          <svg class="animate-spin h-4 w-4" viewBox="0 0 24 24" fill="none"><circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" /><path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" /></svg>
                          Loading...
                        </span>
                      </Show>
                    </button>
                  </div>
                </form>
              </Show>
            </Show>

            {/* Toggle login/register */}
            <Show when={regStep() === 0}>
              <button type="button" onClick={() => { setIsLogin(!isLogin()); setError(''); }} class="w-full py-2 text-sm text-muted-foreground hover:text-foreground transition-colors text-center">
                {isLogin() ? t('auth.noAccount') : t('auth.hasAccount')}{' '}
                <span class="text-primary font-medium underline underline-offset-2">{isLogin() ? t('auth.register') : t('auth.login')}</span>
              </button>
            </Show>
          </div>
        </div>

        <p class="mt-6 text-center text-[11px] text-muted-foreground/40 leading-relaxed">{t('auth.terms')}</p>
      </div>
    </div>
  );
}
