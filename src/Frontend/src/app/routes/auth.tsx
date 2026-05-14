import { createSignal } from 'solid-js';
import { useNavigate } from '@solidjs/router';
import { apiClient, type LoginRequest, type RegisterRequest } from '../../lib/api-client';

export default function Auth() {
  const [email, setEmail] = createSignal('');
  const [displayName, setDisplayName] = createSignal('');
  const [password, setPassword] = createSignal('');
  const [isLogin, setIsLogin] = createSignal(true);
  const [error, setError] = createSignal('');
  const navigate = useNavigate();

  const handleSubmit = async (e: Event) => {
    e.preventDefault();
    setError('');

    try {
      let response;
      if (isLogin()) {
        response = await apiClient.login({ email: email(), password: password() });
      } else {
        response = await apiClient.register({ email: email(), displayName: displayName(), password: password() });
      }

      localStorage.setItem('accessToken', response.accessToken);
      localStorage.setItem('refreshToken', response.refreshToken);

      navigate('/ide');
    } catch (err: any) {
      setError(err.message || 'Authentication failed');
    }
  };

  return (
    <div class="flex items-center justify-center min-h-screen bg-background text-foreground">
      <form
        onSubmit={handleSubmit}
        class="w-full max-w-md p-8 bg-surface rounded-2xl border border-surface-3 shadow-xl space-y-6"
      >
        <h2 class="text-2xl font-bold text-center tracking-tight">
          {isLogin() ? 'Welcome back' : 'Create account'}
        </h2>

        {error() && (
          <div class="p-3 rounded-lg bg-error/10 border border-error/20 text-error text-sm">
            {error()}
          </div>
        )}

        <div class="space-y-4">
          <div>
            <label class="block text-sm font-medium text-muted-foreground mb-1">Email</label>
            <input
              type="email"
              placeholder="you@example.com"
              value={email()}
              onInput={(e) => setEmail(e.currentTarget.value)}
              required
              class="w-full px-4 py-2.5 bg-surface-2 border border-surface-3 rounded-lg text-foreground placeholder:text-muted-foreground/50 focus:outline-none focus:ring-2 focus:ring-primary/50 focus:border-primary transition-all"
            />
          </div>

          {!isLogin() && (
            <div>
              <label class="block text-sm font-medium text-muted-foreground mb-1">Display Name</label>
              <input
                type="text"
                placeholder="Your name"
                value={displayName()}
                onInput={(e) => setDisplayName(e.currentTarget.value)}
                required
                class="w-full px-4 py-2.5 bg-surface-2 border border-surface-3 rounded-lg text-foreground placeholder:text-muted-foreground/50 focus:outline-none focus:ring-2 focus:ring-primary/50 focus:border-primary transition-all"
              />
            </div>
          )}

          <div>
            <label class="block text-sm font-medium text-muted-foreground mb-1">Password</label>
            <input
              type="password"
              placeholder="••••••••"
              value={password()}
              onInput={(e) => setPassword(e.currentTarget.value)}
              required
              class="w-full px-4 py-2.5 bg-surface-2 border border-surface-3 rounded-lg text-foreground placeholder:text-muted-foreground/50 focus:outline-none focus:ring-2 focus:ring-primary/50 focus:border-primary transition-all"
            />
          </div>
        </div>

        <button
          type="submit"
          class="w-full py-3 bg-primary text-primary-foreground font-semibold rounded-lg hover:bg-primary/90 active:scale-[0.98] transition-all"
        >
          {isLogin() ? 'Login' : 'Register'}
        </button>

        <button
          type="button"
          onClick={() => setIsLogin(!isLogin())}
          class="w-full py-2 text-sm text-muted-foreground hover:text-foreground transition-colors"
        >
          {isLogin() ? "Don't have an account? Register" : 'Already have an account? Login'}
        </button>
      </form>
    </div>
  );
}