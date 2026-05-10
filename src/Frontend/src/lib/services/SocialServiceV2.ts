import { apiClient } from '@libr4/shared';

export interface UserProfile {
  name: string;
  bio?: string;
  profileImageUrl?: string;
  location?: string;
}

export interface Post {
  id: string;
  content: string;
  tags: string[];
  likesCount: number;
  commentsCount: number;
  createdAt: string;
}

export interface Connection {
  id: string;
  connectedUserId: string;
  type: string;
  note?: string;
}

export class SocialServiceV2 {
  static readonly BASE_URL = '/api/v2/social';

  // Profile Management
  static async getProfile() {
    return apiClient.get<UserProfile>(`${this.BASE_URL}/profile`);
  }

  static async updateProfile(profile: UserProfile) {
    return apiClient.put<UserProfile>(`${this.BASE_URL}/profile`, profile);
  }

  // Connection Management
  static async getConnections(skip?: number, take?: number) {
    const params = new URLSearchParams();
    if (skip !== undefined) params.append('skip', skip.toString());
    if (take !== undefined) params.append('take', take.toString());
    return apiClient.get<Connection[]>(`${this.BASE_URL}/connections?${params}`);
  }

  static async addConnection(userId: string, type: string, note?: string) {
    return apiClient.post(`${this.BASE_URL}/connections`, { connectedUserId: userId, type, note });
  }

  static async removeConnection(userId: string) {
    return apiClient.delete(`${this.BASE_URL}/connections/${userId}`);
  }

  // Follow Management
  static async followUser(userId: string) {
    return apiClient.post(`${this.BASE_URL}/follow/${userId}`, {});
  }

  static async unfollowUser(userId: string) {
    return apiClient.delete(`${this.BASE_URL}/follow/${userId}`);
  }

  static async getFollowers(skip?: number, take?: number) {
    const params = new URLSearchParams();
    if (skip !== undefined) params.append('skip', skip.toString());
    if (take !== undefined) params.append('take', take.toString());
    return apiClient.get(`${this.BASE_URL}/followers?${params}`);
  }

  static async getFollowing(skip?: number, take?: number) {
    const params = new URLSearchParams();
    if (skip !== undefined) params.append('skip', skip.toString());
    if (take !== undefined) params.append('take', take.toString());
    return apiClient.get(`${this.BASE_URL}/following?${params}`);
  }

  // Post Management
  static async createPost(content: string, tags?: string[], attachments?: string[]) {
    return apiClient.post<{ postId: string }>(`${this.BASE_URL}/posts`, { content, tags, attachmentUrls: attachments });
  }

  static async getPosts(skip: number = 0, take: number = 20) {
    return apiClient.get<Post[]>(`${this.BASE_URL}/posts?skip=${skip}&take=${take}`);
  }

  static async deletePost(postId: string) {
    return apiClient.delete(`${this.BASE_URL}/posts/${postId}`);
  }

  static async getPostDetail(postId: string) {
    return apiClient.get<Post>(`${this.BASE_URL}/posts/${postId}`);
  }

  // Post Interactions
  static async likePost(postId: string) {
    return apiClient.post(`${this.BASE_URL}/posts/${postId}/like`, {});
  }

  static async unlikePost(postId: string) {
    return apiClient.delete(`${this.BASE_URL}/posts/${postId}/like`);
  }

  static async commentOnPost(postId: string, text: string) {
    return apiClient.post(`${this.BASE_URL}/posts/${postId}/comment`, { text });
  }

  static async deleteComment(postId: string, commentId: string) {
    return apiClient.delete(`${this.BASE_URL}/posts/${postId}/comments/${commentId}`);
  }

  static async sharePost(postId: string, message?: string) {
    return apiClient.post(`${this.BASE_URL}/posts/${postId}/share`, { personalMessage: message });
  }

  // Feed and Feed
  static async getFeed(skip: number = 0, take: number = 20) {
    return apiClient.get<Post[]>(`${this.BASE_URL}/feed?skip=${skip}&take=${take}`);
  }

  static async getActivityFeed(skip: number = 0, take: number = 50) {
    return apiClient.get(`${this.BASE_URL}/activity?skip=${skip}&take=${take}`);
  }

  // Discovery
  static async getRecommendations(topN: number = 10) {
    return apiClient.get(`${this.BASE_URL}/recommendations?topN=${topN}`);
  }

  static async searchUsers(query: string, skip: number = 0, take: number = 20) {
    return apiClient.get(`${this.BASE_URL}/search?q=${encodeURIComponent(query)}&skip=${skip}&take=${take}`);
  }

  // Analytics
  static async getProfileAnalytics(userId: string) {
    return apiClient.get(`${this.BASE_URL}/analytics/profile/${userId}`);
  }

  static async getPostsAnalytics() {
    return apiClient.get(`${this.BASE_URL}/analytics/posts`);
  }
}