using Application.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using VeterinaryApi.Common.Abstracions;
using VeterinaryApi.Domain.Users;
using VeterinaryApi.Features.Users;
using VeterinaryApi.Infrastructure.Persistence;

namespace Application.IntegrationTests.TestBases;

public abstract class UsersTestBase : IntegrationTestBase
{
    protected UsersTestBase(PostgresFixture fixture) : base(fixture)
    {
    }

    protected Register.RegisterCommandHandler CreateRegisterHandler(IServiceProvider services)
    {
        return new Register.RegisterCommandHandler(
            services.GetRequiredService<IApplicationDbContext>(),
            services.GetRequiredService<IPasswordHasher>(),
            services.GetRequiredService<IJwtProvider>(),
            services.GetRequiredService<IHttpContextAccessor>());
    }

    protected Login.LoginCommandHandler CreateLoginHandler(IServiceProvider services)
    {
        return new Login.LoginCommandHandler(
            services.GetRequiredService<IApplicationDbContext>(),
            services.GetRequiredService<IPasswordHasher>(),
            services.GetRequiredService<IJwtProvider>(),
            services.GetRequiredService<IHttpContextAccessor>());
    }

    protected RefreshToken.RefreshTokenCommandHandler CreateRefreshTokenHandler(IServiceProvider services)
    {
        return new RefreshToken.RefreshTokenCommandHandler(
            services.GetRequiredService<IApplicationDbContext>(),
            services.GetRequiredService<IJwtProvider>(),
            services.GetRequiredService<IHttpContextAccessor>());
    }

    protected Logout.LogoutCommandHandler CreateLogoutHandler(IServiceProvider services)
    {
        return new Logout.LogoutCommandHandler(
            services.GetRequiredService<IApplicationDbContext>(),
            services.GetRequiredService<IHttpContextAccessor>());
    }

    protected ForgetPassword.ForgetPasswordCommandHandler CreateForgetPasswordHandler(IServiceProvider services)
    {
        return new ForgetPassword.ForgetPasswordCommandHandler(
            services.GetRequiredService<IApplicationDbContext>(),
            services.GetRequiredService<IJwtProvider>(),
            services.GetRequiredService<VeterinaryApi.Common.Abstracions.Emails.IEmailService>());
    }

    protected ResetPassword.ResetPasswordCommandHandler CreateResetPasswordHandler(IServiceProvider services)
    {
        return new ResetPassword.ResetPasswordCommandHandler(
            services.GetRequiredService<IApplicationDbContext>(),
            services.GetRequiredService<IPasswordHasher>());
    }

    protected ChangeEmail.ChangeEmailCommandHandler CreateChangeEmailHandler(IServiceProvider services)
    {
        return new ChangeEmail.ChangeEmailCommandHandler(
            services.GetRequiredService<IApplicationDbContext>(),
            services.GetRequiredService<ICurrentTenant>(),
            services.GetRequiredService<FluentValidation.IValidator<ChangeEmail.ChangeEmailCommand>>());
    }

    protected ChangePassword.ChangePasswordCommandHandler CreateChangePasswordHandler(IServiceProvider services)
    {
        return new ChangePassword.ChangePasswordCommandHandler(
            services.GetRequiredService<IApplicationDbContext>(),
            services.GetRequiredService<ICurrentTenant>(),
            services.GetRequiredService<IPasswordHasher>());
    }

    protected UpdateUserProfile.UpdateUserProfileCommandHandler CreateUpdateUserProfileHandler(IServiceProvider services)
    {
        return new UpdateUserProfile.UpdateUserProfileCommandHandler(
            services.GetRequiredService<IApplicationDbContext>(),
            services.GetRequiredService<ICurrentTenant>());
    }

    protected GetAllUsers.QueryHandler CreateGetAllUsersHandler(IServiceProvider services)
    {
        return new GetAllUsers.QueryHandler(
            services.GetRequiredService<IApplicationDbContext>());
    }

    protected GetUserById.GetUserByIdQueryHandler CreateGetUserByIdHandler(IServiceProvider services)
    {
        return new GetUserById.GetUserByIdQueryHandler(
            services.GetRequiredService<IApplicationDbContext>());
    }

    protected async Task<User> SeedUserAsync(
        ApplicationDbContext db,
        string email = "user@test.local",
        string password = "Pass1234!",
        string userName = "user",
        string firstName = "First",
        string lastName = "Last")
    {
        var hasher = RootProvider.GetRequiredService<IPasswordHasher>();
        var user = User.Register(userName, firstName, lastName, email, hasher.Hash(password));
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    protected async Task<UserSession> SeedRefreshSessionAsync(
        ApplicationDbContext db,
        User user,
        string? token = null,
        DateTime? expiresAt = null)
    {
        var session = new UserSession
        {
            UserId = user.Id,
            Token = token ?? Guid.NewGuid().ToString("N"),
            TokenType = UserSessionTokenType.Refresh,
            ExpiresAt = expiresAt ?? DateTime.UtcNow.AddDays(1)
        };
        db.UserSessions.Add(session);
        await db.SaveChangesAsync();
        return session;
    }

    protected async Task<UserSession> SeedResetSessionAsync(
        ApplicationDbContext db,
        User user,
        string token,
        DateTime expiresAt)
    {
        var session = new UserSession
        {
            UserId = user.Id,
            Token = token,
            TokenType = UserSessionTokenType.ResetPassword,
            ExpiresAt = expiresAt
        };
        db.UserSessions.Add(session);
        await db.SaveChangesAsync();
        return session;
    }
}
