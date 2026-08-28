using Modules.Employees.Contracts;
using Modules.Shared.Endpoints;

namespace PublicApi.Features
{
    public static class Test
    {
        public sealed class TestEmployee : IEndpoint
        {
            public void AddRoutes(IEndpointRouteBuilder app)
            {
                app.MapGet("/employees/id", async (IEmployeeApi api, CancellationToken ct = default) =>
                {
                    var employee = await  api.GetEmployeeByIdAsync("00251X");
                    if (employee is null)
                    {
                        return Results.NotFound("hola, no found ");
                    }
                    return Results.Ok(employee);
                }).WithTags("Test");


                app.MapGet("/employees/Bdg", async (IEmployeeApi api, CancellationToken ct = default) =>
                              {
                                  var employee =await  api.GetEmployeeByBadgeAsync(82,DateOnly.MinValue);
                                  if (employee is null)
                                  {
                                      return Results.NotFound("hola, no found ");
                                  }
                                  return Results.Ok(employee);
                              }).WithTags("Test");


            }
        }
    }
}
