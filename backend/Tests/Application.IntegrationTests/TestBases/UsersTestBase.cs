using Application.IntegrationTests.Infrastructure;
using Identity.Abstracions;
using Identity.Application.Users;
using Identity.Domain.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using VeterinaryApi.Common.Abstracions;
using VeterinaryApi.Common.Abstracions.Emails;
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
            services.GetRequiredService<IIdentityApplicationDbContext>(),
            services.GetRequiredService<IPasswordHasher>(),
            services.GetRequiredService<IJwtProvider>(),
            services.GetRequiredService<IHttpContextAccessor>());
    }

    protected Login.LoginCommandHandler CreateLoginHandler(IServiceProvider services)
    {
        return new Login.LoginCommandHandler(
            services.GetRequiredService<IIdentityApplicationDbContext>(),
            services.GetRequiredService<IPasswordHasher>(),
            services.GetRequiredService<IJwtProvider>(),
            services.GetRequiredService<IHttpContextAccessor>());
    }

    protected RefreshToken.RefreshTokenCommandHandler CreateRefreshTokenHandler(IServiceProvider services)
    {
        return new RefreshToken.RefreshTokenCommandHandler(
            services.GetRequiredService<IIdentityApplicationDbContext>(),
            services.GetRequiredService<IJwtProvider>(),
            services.GetRequiredService<IHttpContextAccessor>());
    }

    protected Logout.LogoutCommandHandler CreateLogoutHandler(IServiceProvider services)
    {
        return new Logout.LogoutCommandHandler(
            services.GetRequiredService<IIdentityApplicationDbContext>(),
            services.GetRequiredService<IHttpContextAccessor>());
    }

    protected ForgetPassword.ForgetPasswordCommandHandler CreateForgetPasswordHandler(IServiceProvider services)
    {
        return new ForgetPassword.ForgetPasswordCommandHandler(
            services.GetRequiredService<IIdentityApplicationDbContext>(),
            services.GetRequiredService<IJwtProvider>(),
            services.GetRequiredService<IEmailService>());
    }

    protected ResetPassword.ResetPasswordCommandHandler CreateResetPasswordHandler(IServiceProvider services)
    {
        return new ResetPassword.ResetPasswordCommandHandler(
            services.GetRequiredService<IIdentityApplicationDbContext>(),
            services.GetRequiredService<IPasswordHasher>());
    }

    protected ChangeEmail.ChangeEmailCommandHandler CreateChangeEmailHandler(IServiceProvider services)
    {
        return new ChangeEmail.ChangeEmailCommandHandler(
            services.GetRequiredService<IIdentityApplicationDbContext>(),
            services.GetRequiredService<ICurrentTenant>(),
            services.GetRequiredService<FluentValidation.IValidator<ChangeEmail.ChangeEmailCommand>>());
    }

    protected ChangePassword.ChangePasswordCommandHandler CreateChangePasswordHandler(IServiceProvider services)
    {
        return new ChangePassword.ChangePasswordCommandHandler(
            services.GetRequiredService<IIdentityApplicationDbContext>(),
            services.GetRequiredService<ICurrentTenant>(),
            services.GetRequiredService<IPasswordHasher>());
    }

    protected UpdateUserProfile.UpdateUserProfileCommandHandler CreateUpdateUserProfileHandler(IServiceProvider services)
    {
        return new UpdateUserProfile.UpdateUserProfileCommandHandler(
            services.GetRequiredService<IIdentityApplicationDbContext>(),
            services.GetRequiredService<ICurrentTenant>());
    }

    protected GetAllUsers.QueryHandler CreateGetAllUsersHandler(IServiceProvider services)
    {
        return new GetAllUsers.QueryHandler(
            services.GetRequiredService<IIdentityApplicationDbContext>());
    }

    protected GetUserById.GetUserByIdQueryHandler CreateGetUserByIdHandler(IServiceProvider services)
    {
        return new GetUserById.GetUserByIdQueryHandler(
            services.GetRequiredService<IIdentityApplicationDbContext>(),
            services.GetRequiredService<IUserSubscriptionStatusQuery>());
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
