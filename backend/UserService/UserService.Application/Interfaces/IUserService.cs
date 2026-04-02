namespace UserService.Application.Interfaces;

using UserService.Application.DTOs;

public interface IUserService
{
    Task<Guid> CreateUser(Guid userId, CreateUserRequest request);

    Task<UserResponse?> GetByUserId(Guid userId);

    Task<UserResponse?> GetById(Guid id);

    Task<List<UserResponse>> GetAll();

    Task UpdateUser(Guid userId, UpdateUserRequest request);

    Task DeactivateUser(Guid userId);

    Task DeleteUser(Guid id);
}