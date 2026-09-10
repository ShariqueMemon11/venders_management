using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Platform.Workflow.Entities;

namespace Platform.Workflow.Configurations;

public class BusinessHolidayConfiguration : IEntityTypeConfiguration<BusinessHoliday>
{
    public void Configure(EntityTypeBuilder<BusinessHoliday> builder)
    {
        builder.ToTable("BusinessHolidays");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Description)
               .HasMaxLength(200);

        builder.HasIndex(e => e.Date);
    }
}