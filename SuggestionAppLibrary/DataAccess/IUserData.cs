namespace SuggestionAppLibrary.DataAccess;

public interface IUserData
{
    Task CreateUser(UserModel user);
    Task<UserModel> GetUserById(string userId);
    Task<UserModel> GetUserFromAuthentication(string objectId);
    Task<List<UserModel>> GetUsers();
    Task UpdateUser(UserModel user);
}