import { onMount, type Component } from "solid-js";
import { useNavigate, useSearchParams } from "@solidjs/router";

const AuthCallback: Component = () => {
  const navigate = useNavigate();
  const [params] = useSearchParams();

  onMount(() => {
    const accessToken  = params.token || params.access_token || params.accessToken;
    const refreshToken = params.refresh_token || params.refreshToken;
    const error        = params.error;

    if (error) {
      navigate(`/auth?error=${encodeURIComponent(error as string)}`);
      return;
    }

    if (!accessToken) {
      navigate('/auth?error=no_token');
      return;
    }

    localStorage.setItem('accessToken', accessToken as string);
    if (refreshToken) localStorage.setItem('refreshToken', refreshToken as string);

    // Decode JWT payload to extract user fields without extra request
    try {
      const payload = JSON.parse(atob((accessToken as string).split('.')[1]));
      localStorage.setItem('userId',      payload.sub           ?? '');
      localStorage.setItem('email',       payload.email         ?? '');
      localStorage.setItem('displayName', payload.display_name  ?? payload.displayName ?? '');
      localStorage.setItem('role',        payload.role          ?? '');
    } catch {}

    navigate('/dashboard');
  });

  return (
    <div class="flex flex-col items-center justify-center min-h-screen bg-background gap-4">
      <div class="w-12 h-12 rounded-2xl bg-primary/10 border border-primary/20 flex items-center justify-center">
        <span class="text-primary font-bold">L4</span>
      </div>
      <p class="text-sm text-muted-foreground animate-pulse">Completing sign in...</p>
    </div>
  );
};

export default AuthCallback;
