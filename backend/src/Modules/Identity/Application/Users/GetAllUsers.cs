using Modules.Identity.Abstracions;
using Modules.Identity.Domain.Users;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using Modules.Shared.CQRS;
using Modules.Shared.Endpoints;
using Modules.Shared.Paginations.OffSet;
using Modules.Shared.Results;
using static Modules.Identity.Application.Users.Shared;

namespace Modules.Identity.Application.Users;

public static class GetAllUsers
{
    public sealed class QueryHandler(
         IIdentityApplicationDbContext db)
         : IQueryHandler<TableRequest<Response>, OffSetPagedList<Response>>
    {
        public async Task<Result<OffSetPagedList<Response>>> Handle(
            TableRequest<Response> query,
            CancellationToken cancellationToken = default)
        {
            var usersQuery = db.Users
                .AsQueryable();
            if (!string.IsNullOrWhiteSpace(query.search))
            {
                usersQuery = usersQuery.Where(u =>
                    u.UserName.ToLower().Contains(query.search) ||
                    u.FirstName.ToLower().Contains(query.search) ||
                    u.LastName.ToLower().Contains(query.search) ||
                    u.Email.ToLower().Contains(query.search));
            }
            var responseQuery = usersQuery.Select(e => new Response(
                e.Id,
                e.UserName,
                e.FullName,
                e.Email,
                e.Role.ToString(),
                e.IsActive,
                null,
                null,
                e.CreatedOnUtc
            ));
            Expression<Func<Response, object>>? orderBy = query.SortColumn?.ToLower() switch
            {
                "username" => r => r.UserName,
                "fullname" => r => r.FullName,
                "email" => r => r.Email,
                "role" => r => r.Role,
                "isactive" => r => r.IsActive,
                _ => r => r.CreatedOnUtc,
            };
            var temp = await responseQuery.ToListAsync(cancellationToken);
            var tempQuery = temp.AsQueryable();
            if (query.SortOrder?.ToLower() == "desc")
            {
                tempQuery = tempQuery.OrderByDescending(orderBy);
            }
            else
            {
                tempQuery = tempQuery.OrderBy(orderBy);
            }
            var items = tempQuery
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToList();
            var result = OffSetPagedList<Response>.Create(
                items,
                temp.Count,
                query.Page,
                query.PageSize);
            return Result<OffSetPagedList<Response>>.Success(result);
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/users", async (
                           [FromQuery] int? page,
                           [FromQuery] int? pageSize,
                           [FromQuery] string? sortColumn,
                           [FromQuery] string? sortOrder,
                           [FromQuery] string? search,
                           [FromServices] IQueryHandler<TableRequest<Response>,
                           OffSetPagedList<Response>> handler,
                           CancellationToken cancellationToken) =>
                       {
                           var query = TableRequest<Response>
                               .Create(pageSize, page, search, sortColumn, sortOrder);

                           var result = await handler.Handle(query, cancellationToken);
                           return result.IsSuccess ? Results.Ok(result.Value) :
                               result.Problem();

                       }).RequireAuthorization();
        }
    }
}
