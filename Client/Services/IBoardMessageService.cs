using BlazorApp1.Shared;
using BlazorApp1.Shared.Models;

namespace BlazorApp1.Client.Services
{
    public interface IBoardMessageService
    {
        Task<Response<List<BoardMessageDto>>> GetMessagesAsync();
        Task<Response<BoardMessageDto>> CreateMessageAsync(BoardMessageDto message);
        Task<Response> MarkMessageAsReadAsync(int messageId);
        Task<Response> ClearAllMessagesAsync();
    }
} 