import { apiClient } from '@libr4/shared';

export class SocialService {
  static async getProfile() {
    return apiClient.get('/api/social/profile');
  }

  static async updateProfile(name: string, bio: string, imageUrl: string, location: string) {
    return apiClient.put('/api/social/profile', { name, bio, profileImageUrl: imageUrl, location });
  }

  static async getConnections() {
    return apiClient.get('/api/social/connections');
  }

  static async addConnection(userId: string, type: string, note?: string) {
    return apiClient.post('/api/social/connections', { connectedUserId: userId, type, note });
  }

  static async removeConnection(userId: string) {
    return apiClient.delete(`/api/social/connections/${userId}`);
  }

  static async followUser(userId: string) {
    return apiClient.post(`/api/social/follow/${userId}`, {});
  }

  static async unfollowUser(userId: string) {
    return apiClient.delete(`/api/social/follow/${userId}`);
  }

  static async getFeed(skip: number = 0, take: number = 20) {
    return apiClient.get(`/api/social/feed?skip=${skip}&take=${take}`);
  }

  static async createPost(content: string, tags?: string[], attachments?: string[]) {
    return apiClient.post('/api/social/posts', { content, tags, attachmentUrls: attachments });
  }

  static async deletePost(postId: string) {
    return apiClient.delete(`/api/social/posts/${postId}`);
  }

  static async likePost(postId: string) {
    return apiClient.post(`/api/social/posts/${postId}/like`, {});
  }

  static async commentOnPost(postId: string, text: string) {
    return apiClient.post(`/api/social/posts/${postId}/comment`, { text });
  }

  static async sharePost(postId: string, message?: string) {
    return apiClient.post(`/api/social/posts/${postId}/share`, { personalMessage: message });
  }

  static async getRecommendations(topN: number = 10) {
    return apiClient.get(`/api/social/recommendations?topN=${topN}`);
  }
}