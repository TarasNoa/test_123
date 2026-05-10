import { createSignal } from 'solid-js';
import { useNavigate } from '@solidjs/router';
import { apiClient, type LoginRequest, type RegisterRequest } from '../../lib/api-client';

export default function Auth() {
  const [email, setEmail] = createSignal('');
  const [username, setUsername] = createSignal('');
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
        response = await apiClient.register({ email: email(), username: username(), password: password() });
      }

      // Store tokens (in real app, use secure storage)
      localStorage.setItem('accessToken', response.accessToken);
      localStorage.setItem('refreshToken', response.refreshToken);

      navigate('/dashboard');
    } catch (err: any) {
      setError(err.message || 'Authentication failed');
    }
  };

  return (
    <div class="auth-page">
      <form onSubmit={handleSubmit} class="auth-form">
        <h2>{isLogin() ? 'Login' : 'Register'}</h2>
        {error() && <p class="error">{error()}</p>}
        <input
          type="email"
          placeholder="Email"
          value={email()}
          onInput={(e) => setEmail(e.currentTarget.value)}
          required
        />
        {!isLogin() && (
          <input
            type="text"
            placeholder="Username"
            value={username()}
            onInput={(e) => setUsername(e.currentTarget.value)}
            required
          />
        )}
        <input
          type="password"
          placeholder="Password"
          value={password()}
          onInput={(e) => setPassword(e.currentTarget.value)}
          required
        />
        <button type="submit">{isLogin() ? 'Login' : 'Register'}</button>
        <button type="button" onClick={() => setIsLogin(!isLogin())}>
          {isLogin() ? 'Need to register?' : 'Already have an account?'}
        </button>
      </form>
    </div>
  );
}