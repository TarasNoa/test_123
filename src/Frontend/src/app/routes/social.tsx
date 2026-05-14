import { createSignal, onMount, For, Show } from 'solid-js';
import { UserProfile } from '../../widgets/social/UserProfile';
import { PostFeed } from '../../widgets/social/PostFeed';
import { UserConnections } from '../../widgets/social/UserConnections';
import { ActivityFeed } from '../../widgets/social/ActivityFeed';

export default function Social() {
  const [activeTab, setActiveTab] = createSignal('feed'); // feed, profile, connections, activity
  const [profile, setProfile] = createSignal(null);
  const [posts, setPosts] = createSignal([]);
  const [connections, setConnections] = createSignal([]);
  const [followers, setFollowers] = createSignal([]);
  const [following, setFollowing] = createSignal([]);
  const [activities, setActivities] = createSignal([]);
  const [recommendations, setRecommendations] = createSignal([]);

  onMount(async () => {
    await loadProfile();
    await loadPosts();
    await loadConnections();
    await loadActivityFeed();
    await loadRecommendations();
  });

  const loadProfile = async () => {
    const res = await fetch('/api/v1/social/profile', {
      headers: { 'Authorization': `Bearer ${localStorage.getItem('accessToken')}` }
    });
    const data = await res.json();
    setProfile(data.profile);
  };

  const loadPosts = async () => {
    const res = await fetch('/api/v1/social/feed', {
      headers: { 'Authorization': `Bearer ${localStorage.getItem('accessToken')}` }
    });
    const data = await res.json();
    setPosts(data.feed || []);
  };

  const loadConnections = async () => {
    const res = await fetch('/api/v1/social/connections', {
      headers: { 'Authorization': `Bearer ${localStorage.getItem('accessToken')}` }
    });
    const data = await res.json();
    setConnections(data.connections || []);
  };

  const loadActivityFeed = async () => {
    const res = await fetch('/api/v1/social/activity', {
      headers: { 'Authorization': `Bearer ${localStorage.getItem('accessToken')}` }
    });
    const data = await res.json();
    setActivities(data.activities || []);
  };

  const loadRecommendations = async () => {
    const res = await fetch('/api/v1/social/recommendations?topN=5', {
      headers: { 'Authorization': `Bearer ${localStorage.getItem('accessToken')}` }
    });
    const data = await res.json();
    setRecommendations(data.recommendations || []);
  };

  const updateProfile = async (name, bio, imageUrl, location) => {
    await fetch('/api/v1/social/profile', {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${localStorage.getItem('accessToken')}`
      },
      body: JSON.stringify({ name, bio, profileImageUrl: imageUrl, location })
    });
    await loadProfile();
  };

  const createPost = async (content, tags, attachments) => {
    await fetch('/api/v1/social/posts', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${localStorage.getItem('accessToken')}`
      },
      body: JSON.stringify({ content, tags, attachmentUrls: attachments })
    });
    await loadPosts();
  };

  const likePost = async (postId) => {
    await fetch(`/api/v1/social/posts/${postId}/like`, {
      method: 'POST',
      headers: { 'Authorization': `Bearer ${localStorage.getItem('accessToken')}` }
    });
    await loadPosts();
  };

  const commentOnPost = async (postId, text) => {
    await fetch(`/api/v1/social/posts/${postId}/comment`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${localStorage.getItem('accessToken')}`
      },
      body: JSON.stringify({ text })
    });
    await loadPosts();
  };

  const sharePost = async (postId, message) => {
    await fetch(`/api/v1/social/posts/${postId}/share`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${localStorage.getItem('accessToken')}`
      },
      body: JSON.stringify({ personalMessage: message })
    });
    await loadPosts();
  };

  const followUser = async (userId) => {
    await fetch(`/api/v1/social/follow/${userId}`, {
      method: 'POST',
      headers: { 'Authorization': `Bearer ${localStorage.getItem('accessToken')}` }
    });
    await loadFollowing();
  };

  const unfollowUser = async (userId) => {
    await fetch(`/api/v1/social/follow/${userId}`, {
      method: 'DELETE',
      headers: { 'Authorization': `Bearer ${localStorage.getItem('accessToken')}` }
    });
    await loadFollowing();
  };

  const loadFollowing = async () => {
    const res = await fetch('/api/v1/social/following', {
      headers: { 'Authorization': `Bearer ${localStorage.getItem('accessToken')}` }
    });
    const data = await res.json();
    setFollowing(data.following || []);
  };

  return (
    <div class="social-page">
      <div class="social-tabs">
        <button onClick={() => setActiveTab('feed')} class={activeTab() === 'feed' ? 'active' : ''}>📰 Feed</button>
        <button onClick={() => setActiveTab('profile')} class={activeTab() === 'profile' ? 'active' : ''}>👤 Profile</button>
        <button onClick={() => setActiveTab('connections')} class={activeTab() === 'connections' ? 'active' : ''}>👥 Connections</button>
        <button onClick={() => setActiveTab('activity')} class={activeTab() === 'activity' ? 'active' : ''}>⏰ Activity</button>
      </div>

      <div class="social-content">
        <Show when={activeTab() === 'feed'}>
          <div class="feed-section">
            <PostFeed 
              posts={posts()} 
              onLike={likePost}
              onComment={commentOnPost}
              onShare={sharePost}
            />
          </div>
          
          <aside class="recommendations-sidebar">
            <h3>Recommendations</h3>
            <For each={recommendations()}>
              {(user) => (
                <div class="recommendation-card">
                  <h4>{user.profile.name}</h4>
                  <p>{user.followerCount} followers</p>
                  <button onClick={() => followUser(user.userId)}>Follow</button>
                </div>
              )}
            </For>
          </aside>
        </Show>

        <Show when={activeTab() === 'profile'}>
          <UserProfile 
            profile={profile()} 
            onUpdate={updateProfile}
            onCreatePost={createPost}
          />
        </Show>

        <Show when={activeTab() === 'connections'}>
          <UserConnections 
            connections={connections()} 
            followers={followers()}
            following={following()}
            onFollow={followUser}
            onUnfollow={unfollowUser}
          />
        </Show>

        <Show when={activeTab() === 'activity'}>
          <ActivityFeed activities={activities()} />
        </Show>
      </div>
    </div>
  );
}