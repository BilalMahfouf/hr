using System;
using System.Collections.Generic;
using System.Text;

namespace Modules.Shared.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Modules.Shared.Domain.Common;

public static class EntityTypeBuilderExtensions
{
    public static EntityTypeBuilder<T> IgnoreSoftDelete<T>(
        this EntityTypeBuilder<T> builder)
        where T : Entity
    {
        builder.Ignore(x => x.IsDeleted);
        builder.Ignore(x => x.DeletedOnUtc);

        return builder;
    }
}
