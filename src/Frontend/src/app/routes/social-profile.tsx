import { createSignal, onMount, Show, type Component } from 'solid-js';
import { useParams, useNavigate } from '@solidjs/router';
import { apiClient } from '../../lib/api-client';

const SocialProfile: Component = () => {
  const params = useParams();
  const navigate = useNavigate();
  const [profile, setProfile] = createSignal<{ name: string; bio?: string; profileImageUrl?: string; location?: string } | null>(null);
  const [loading, setLoading] = createSignal(true);
  const [error, setError] = createSignal('');

  onMount(async () => {
    const userId = params.id;
    if (!userId) {
      setError('User ID is missing');
      setLoading(false);
      return;
    }

    try {
      const data = await apiClient.getUserProfile(userId);
      setProfile(data);
    } catch (e) {
      setError('Failed to load profile');
    } finally {
      setLoading(false);
    }
  });

  return (
    <div class="min-h-screen bg-background text-foreground">
      <div class="max-w-2xl mx-auto px-4 py-8">
        <button
          onClick={() => navigate('/dashboard')}
          class="mb-4 text-sm text-muted-foreground hover:text-foreground"
        >
          ← Back
        </button>

        <Show when={loading()}>
          <div class="flex items-center justify-center py-20">
            <div class="animate-pulse text-secondary">Loading profile...</div>
          </div>
        </Show>

        <Show when={error()}>
          <div class="text-center py-20 text-red-400">{error()}</div>
        </Show>

        <Show when={!loading() && profile()}>
          <div class="bg-surface-1 rounded-2xl p-6 border border-surface-3">
            <div class="flex items-start gap-4">
              <div class="w-20 h-20 rounded-full bg-white/10 flex items-center justify-center text-2xl font-bold shrink-0">
                {profile()?.profileImageUrl ? (
                  <img src={profile()?.profileImageUrl} alt="" class="w-full h-full rounded-full object-cover" />
                ) : (
                  profile()?.name?.[0]?.toUpperCase() ?? '?'
                )}
              </div>
              <div class="flex-1 min-w-0">
                <h1 class="text-2xl font-bold truncate">{profile()?.name}</h1>
                <Show when={profile()?.location}>
                  <p class="text-sm text-muted-foreground mt-1">{profile()?.location}</p>
                </Show>
              </div>
            </div>

            <Show when={profile()?.bio}>
              <div class="mt-6">
                <h2 class="text-sm font-semibold text-muted-foreground uppercase tracking-wide mb-2">Bio</h2>
                <p class="text-foreground leading-relaxed">{profile()?.bio}</p>
              </div>
            </Show>
          </div>
        </Show>
      </div>
    </div>
  );
};

export default SocialProfile;
