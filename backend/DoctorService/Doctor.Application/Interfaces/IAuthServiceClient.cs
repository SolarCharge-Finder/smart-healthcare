namespace Doctor.Application.Interfaces;
public interface IAuthServiceClient
{
    Task GrantRoleAsync(Guid userId, string role);
}