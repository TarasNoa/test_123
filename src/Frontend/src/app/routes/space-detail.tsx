import { createSignal, onMount, Show, type Component } from 'solid-js';
import { useNavigate } from '@solidjs/router';
import { SpaceDetail } from '../../features/IDE/AgentSpaces/SpaceDetail';

const SpaceDetailPage: Component = () => {
  const navigate = useNavigate();
  const [authorized, setAuthorized] = createSignal(false);

  onMount(() => {
    const token = localStorage.getItem('accessToken');
    if (!token) {
      navigate('/auth');
      return;
    }
    setAuthorized(true);
  });

  return (
    <Show when={authorized()} fallback={<div class="p-4 text-xs text-muted-foreground">Loading space…</div>}>
      <SpaceDetail />
    </Show>
  );
};

export default SpaceDetailPage;
