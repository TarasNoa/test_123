namespace Libr4.IDE.Domain.AI;

public interface IAIConversationRepository
{
    Task<AIConversation?> GetByIdAsync(Guid id);
    Task<AIConversation?> GetByIdWithMessagesAsync(Guid id);
    Task<List<AIConversation>> GetByUserIdAsync(Guid userId, int skip = 0, int limit = 20, bool archivedOnly = false);
    Task<List<AIMessage>> GetMessagesByConversationIdAsync(Guid conversationId);
    Task AddAsync(AIConversation conversation);
    Task UpdateAsync(AIConversation conversation);
    Task DeleteAsync(AIConversation conversation);
    Task<bool> ExistsAsync(Guid id);
}

public interface IAITemplateRepository
{
    Task<AITemplate?> GetByIdAsync(Guid id);
    Task<List<AITemplate>> GetByCreatorIdAsync(Guid creatorId);
    Task<List<AITemplate>> GetPublicAsync(string? category = null);
    Task AddAsync(AITemplate template);
    Task UpdateAsync(AITemplate template);
    Task DeleteAsync(AITemplate template);
    Task<bool> ExistsAsync(Guid id);
}

public interface IAIWorkflowRepository
{
    Task<AIWorkflow?> GetByIdAsync(Guid id);
    Task<List<AIWorkflow>> GetByUserIdAsync(Guid userId);
    Task AddAsync(AIWorkflow workflow);
    Task UpdateAsync(AIWorkflow workflow);
    Task DeleteAsync(AIWorkflow workflow);
    Task<bool> ExistsAsync(Guid id);
}

public interface ISmartSuggestionRepository
{
    Task<SmartSuggestion?> GetByIdAsync(Guid id);
    Task<List<SmartSuggestion>> GetByUserIdAsync(Guid userId, bool unreadOnly = true);
    Task<List<SmartSuggestion>> GetByProjectIdAsync(Guid projectId);
    Task AddAsync(SmartSuggestion suggestion);
    Task UpdateAsync(SmartSuggestion suggestion);
    Task DeleteAsync(SmartSuggestion suggestion);
    Task<bool> ExistsAsync(Guid id);
}
