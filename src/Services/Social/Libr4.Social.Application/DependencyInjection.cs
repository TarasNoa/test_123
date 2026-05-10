using Microsoft.Extensions.DependencyInjection;
using Libr4.Social.Application.Abstractions;
using Libr4.Social.Application.Commands;
using Libr4.Social.Application.Queries;
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
        services.AddScoped<ICommandHandler<CommentOnPostCommand>, CommentOnPostCommandHandler>();
        services.AddScoped<ICommandHandler<SharePostCommand>, SharePostCommandHandler>();
        services.AddScoped<ICommandHandler<UpdateProfileCommand>, UpdateProfileCommandHandler>();
        services.AddScoped<ICommandHandler<AddConnectionCommand>, AddConnectionCommandHandler>();

        // Query Handlers
        services.AddScoped<IQueryHandler<GetUserPostsQuery, List<UserPostDto>>, GetUserPostsQueryHandler>();
        services.AddScoped<IQueryHandler<GetUserProfileQuery, UserProfileDto>, GetUserProfileQueryHandler>();
        services.AddScoped<IQueryHandler<GetConnectionsQuery, List<SocialConnectionDto>>, GetConnectionsQueryHandler>();
        services.AddScoped<IQueryHandler<GetFeedQuery, List<UserPostDto>>, GetFeedQueryHandler>();
        services.AddScoped<IQueryHandler<GetActivityFeedQuery, List<UserActivityDto>>, GetActivityFeedQueryHandler>();
        services.AddScoped<IQueryHandler<GetRecommendedConnectionsQuery, List<SocialNetworkDto>>, GetRecommendedConnectionsQueryHandler>();

        // Event Handlers
        services.AddScoped<IEventHandler<PostCreatedEvent>, PostCreatedEventHandler>();
        services.AddScoped<IEventHandler<FollowerAddedEvent>, FollowerAddedEventHandler>();
        services.AddScoped<IEventHandler<ConnectionAddedEvent>, ConnectionAddedEventHandler>();
        services.AddScoped<IEventHandler<ProfileUpdatedEvent>, ProfileUpdatedEventHandler>();

        return services;
    }
}