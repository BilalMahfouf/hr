using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace PublicApi.Infrastructure.Notifications;

/// <summary>
/// SignalR hub that serves as the real-time communication endpoint for client notifications.
/// </summary>
/// <remarks>
/// The hub is mapped at <c>/hubs/notification</c> in <c>Program.cs</c> and is protected by
/// the <see cref="AuthorizeAttribute"/> so that only authenticated users can connect.
///
/// <b>JWT via query string:</b> The SignalR JavaScript client cannot attach custom HTTP headers
/// during the WebSocket handshake. To accommodate this, the JWT bearer middleware is configured
/// to read the token from the <c>access_token</c> query-string parameter for requests targeting
/// this hub path.
///
/// The hub itself contains no additional methods; all server-to-client messaging is performed
/// by <see cref="NotificationService"/> via <see cref="IHubContext{THub}"/>.
///
/// <b>User targeting:</b> SignalR uses the authenticated user's <c>NameIdentifier</c> claim as the
/// user identifier, which allows <c>Clients.User(userId.ToString())</c> routing across connections.
/// </remarks>
[Authorize]
public class NotificationHub : Hub
{
}
