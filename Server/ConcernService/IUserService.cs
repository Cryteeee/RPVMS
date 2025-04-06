using System.Threading.Tasks;

namespace BlazorApp1.Server.Services
{
    public interface IUserService
    {
        Task<bool> IsEmailUniqueAsync(string email);
        Task<bool> IsUsernameUniqueAsync(string username);
    }
}
