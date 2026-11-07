using Application.Common.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class AdminRoleConfiguration : IEntityTypeConfiguration<IdentityRole>
{
    public void Configure(EntityTypeBuilder<IdentityRole> builder) => builder.HasData(new IdentityRole
    {
        Id = "f8d9817a-778a-42c6-a54b-e1c66055b68d",
        Name = UserRoles.Admin,
        NormalizedName = "ADMIN",
        ConcurrencyStamp = "f8d9817a-778a-42c6-a54b-e1c66055b68d"
    });
}
