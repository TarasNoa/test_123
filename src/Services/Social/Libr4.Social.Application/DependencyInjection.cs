using Microsoft.Extensions.DependencyInjection;
using Libr4.Social.Application.Abstractions;
using Libr4.Social.Application.Commands;
using Libr4.Social.Application.Queries;
using Libr4.Social.Application.EventHandlers;
using Libr4.Social.Domain.Events;
using Libr4.Shared.Infrastructure.Messaging;
using MediatR;

namespace Libr4.Social.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddSocialApplication(this IServiceCollection services)
    {
        // Application Service
        services.AddScoped<ISocialNetworkService, SocialNetworkService>();

        // Command Handlers
        services.AddScoped<ICommandHandler<CreatePostCommand, Guid>, CreatePostCommandHandler>();
        services.AddScoped<ICommandHandler<FollowUserCommand>, FollowUserCommandHandler>();
        services.AddScoped<ICommandHandler<UnfollowUserCommand>, UnfollowUserCommandHandler>();
        services.AddScoped<ICommandHandler<LikePostCommand>, LikePostCommandHandler>();
        services.AddScoped<ICommandHandler<UnlikePostCommand>, UnlikePostCommandHandler>();
        services.AddScoped<ICommandHandler<CommentOnPostCommand>, CommentOnPostCommandHandler>();
        services.AddScoped<ICommandHandler<SharePostCommand>, SharePostCommandHandler>();
        services.AddScoped<ICommandHandler<UpdateProfileCommand>, UpdateProfileCommandHandler>();
        services.AddScoped<ICommandHandler<AddConnectionCommand>, AddConnectionCommandHandler>();
        services.AddScoped<ICommandHandler<RemoveConnectionCommand>, RemoveConnectionCommandHandler>();
        services.AddScoped<ICommandHandler<DeletePostCommand>, DeletePostCommandHandler>();
        services.AddScoped<ICommandHandler<DeleteCommentCommand>, DeleteCommentCommandHandler>();

        // Query Handlers
        services.AddScoped<IQueryHandler<GetUserPostsQuery, List<UserPostDto>>, GetUserPostsQueryHandler>();
        services.AddScoped<IQueryHandler<GetUserProfileQuery, UserPublicProfileDto>, GetUserProfileQueryHandler>();
        services.AddScoped<IQueryHandler<GetConnectionsQuery, List<SocialConnectionDto>>, GetConnectionsQueryHandler>();
        services.AddScoped<IQueryHandler<GetFeedQuery, List<UserPostDto>>, GetFeedQueryHandler>();
        services.AddScoped<IQueryHandler<GetActivityFeedQuery, List<UserActivityDto>>, GetActivityFeedQueryHandler>();
        services.AddScoped<IQueryHandler<GetRecommendedConnectionsQuery, List<RecommendedUserDto>>, GetRecommendedConnectionsQueryHandler>();
        services.AddScoped<IQueryHandler<GetFollowersQuery, List<SocialNetworkDto>>, GetFollowersQueryHandler>();
        services.AddScoped<IQueryHandler<GetFollowingQuery, List<SocialNetworkDto>>, GetFollowingQueryHandler>();
        services.AddScoped<IQueryHandler<GetPostDetailQuery, PostDetailDto>, GetPostDetailQueryHandler>();
        services.AddScoped<IQueryHandler<SearchUsersQuery, List<UserSearchResultDto>>, SearchUsersQueryHandler>();
        services.AddScoped<IQueryHandler<GetProfileAnalyticsQuery, ProfileAnalyticsDto>, GetProfileAnalyticsQueryHandler>();
        services.AddScoped<IQueryHandler<GetPostsAnalyticsQuery, PostsAnalyticsDto>, GetPostsAnalyticsQueryHandler>();

        // Event Handlers
        services.AddScoped<IEventHandler<PostCreatedEvent>, PostCreatedEventHandler>();
        services.AddScoped<IEventHandler<FollowerAddedEvent>, FollowerAddedEventHandler>();
        services.AddScoped<IEventHandler<ConnectionAddedEvent>, ConnectionAddedEventHandler>();
        services.AddScoped<IEventHandler<ProfileUpdatedEvent>, ProfileUpdatedEventHandler>();

        return services;
    }
}