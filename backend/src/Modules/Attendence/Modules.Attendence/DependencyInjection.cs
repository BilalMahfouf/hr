using Microsoft.Extensions.DependencyInjection;
using Modules.Attendence.Application.Shared;
using Modules.Attendence.Infrastructure.Presistance;
using Modules.Shared.Infrastructure.Outbox;
using System;
using System.Collections.Generic;
using System.Text;

namespace Modules.Attendence;

public static class DependencyInjection
{
    public static IServiceCollection AddAttendenceModule(this IServiceCollection services)
    {
        return services;

    }
}
