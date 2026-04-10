namespace Auth.Application.Services;

using Auth.Application.DTOs;
using Auth.Application.Interfaces;
using Auth.Application.Utilities;
using Auth.Domain.Entities;
using Auth.Domain.Enums;

public class AuthService : IAuthService
{
    private readonly IUserRepository _repo;
    private readonly ITokenService _tokenService;
    private readonly IVerificationService _verificationService;
    private readonly IEmailService _emailService;

    public AuthService(IUserRepository repo, ITokenService tokenService, IVerificationService verificationService, IEmailService emailService)
    {
        _repo = repo;
        _tokenService = tokenService;
        _verificationService = verificationService;
        _emailService = emailService;
    }

    public async Task Register(RegisterRequest request)
    {
        var email = request.Email.ToLower().Trim();

        var recent = await _repo.GetRecentPendingByEmailAsync(email);

        if (recent != null && recent.ExpiresAt > DateTime.UtcNow)
        {
            throw new InvalidOperationException("A verification email has already been sent to this address. Please check your email or wait before trying again.");
        }

        var exists = await _repo.ExistsByEmailAsync(email); //fist check if user already exists

        if (exists)
        {
            throw new InvalidOperationException("User already exists");
        }

        if (request.Role == UserRole.Doctor)
        {
            throw new InvalidOperationException("Cannot self-register as doctor");
        }

        if (request.Role == UserRole.Admin)
        {
            throw new InvalidOperationException("Cannot self-register as admin");
        }

        var existingPending = await _repo.GetPendingByEmailAsync(email);

        // remove any existing pending registration for this email to avoid confusion with multiple tokens
        if (existingPending != null)
        {
            _repo.RemovePending(existingPending);
        }

        // generate a new verification token for this registration attempt
        var verificationToken = _verificationService.GenerateVerificationToken();
        var hashVerificationToken = TokenHasher.Hash(verificationToken);

        var pendingUser = new PendingUser
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = request.Role,
            VerificationToken = hashVerificationToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15) // token valid for 15 minutes
        };

        await _repo.AddPendingAsync(pendingUser);
        await _repo.SaveChangesAsync();

        await _emailService.SendVerificationEmail(email, verificationToken);
    }

    public async Task Verify(string token)
    {
        var decodedToken = Uri.UnescapeDataString(token);
        var hashVerificationToken = TokenHasher.Hash(decodedToken);

        // find the pending user associated with this token
        var pending = await _repo.GetPendingByTokenAsync(hashVerificationToken);

        if (pending == null)
        {
            throw new InvalidOperationException("Invalid token");
        }

        if (pending.ExpiresAt < DateTime.UtcNow)
        {
            _repo.RemovePending(pending);
            await _repo.SaveChangesAsync();

            throw new InvalidOperationException("Token expired");
        }

        // race condition check: if user already exists with this email, remove pending and throw error
        var exists = await _repo.ExistsByEmailAsync(pending.Email);
        if (exists)
        {
            _repo.RemovePending(pending);
            await _repo.SaveChangesAsync();

            throw new InvalidOperationException("User already exists");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = pending.Name,
            Email = pending.Email,
            PasswordHash = pending.PasswordHash,
            Role = pending.Role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _repo.AddAsync(user);

        _repo.RemovePending(pending);

        await _repo.SaveChangesAsync();
    }
    public async Task<LoginResponse?> Login(LoginRequest request)
    {
        var email = request.Email.ToLower().Trim();

        var user = await _repo.GetByEmailAsync(email);

        if (user == null)
        {
            throw new UnauthorizedAccessException("Invalid credentials");
        }

        var valid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);

        if (!valid)
        {
            throw new UnauthorizedAccessException("Invalid credentials");
        }

        var token = _tokenService.GenerateToken(user);

        var response = new LoginResponse
        {
            Token = token,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role.ToString()
        };

        return response;
    }

    public async Task ForgotPassword(ForgotPasswordRequest request)
    {
        var email = request.Email.ToLower().Trim();

        var user = await _repo.GetByEmailAsync(email);

        if (user == null)
        {
            return; // don't reveal whether email exists
        }

        var resetToken = _verificationService.GenerateVerificationToken();
        var hashResetToken = TokenHasher.Hash(resetToken);

        user.PasswordResetToken = hashResetToken;
        user.PasswordResetExpiresAt = DateTime.UtcNow.AddMinutes(15); // token valid for 15 minutes

        await _repo.SaveChangesAsync();

        await _emailService.SendPasswordResetEmail(email, resetToken);
    }

    public async Task ResetPassword(ResetPasswordRequest request)
    {
        var decodedToken = Uri.UnescapeDataString(request.Token);
        var hashResetToken = TokenHasher.Hash(decodedToken);

        var user = await _repo.GetByPasswordResetTokenAsync(hashResetToken);

        if (user == null)
        {
            throw new InvalidOperationException("Invalid token");
        }

        if (user.PasswordResetExpiresAt < DateTime.UtcNow)
        {
            user.PasswordResetToken = null;
            user.PasswordResetExpiresAt = null;
            await _repo.SaveChangesAsync();

            throw new InvalidOperationException("Token expired");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.PasswordResetToken = null;
        user.PasswordResetExpiresAt = null;

        await _repo.SaveChangesAsync();
    }
}
