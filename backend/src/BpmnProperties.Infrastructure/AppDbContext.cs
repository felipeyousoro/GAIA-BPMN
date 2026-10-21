using BpmnProperties.Domain;
using Microsoft.EntityFrameworkCore;

namespace BpmnProperties.Infrastructure;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Process> Processes => Set<Process>();
    public DbSet<BpmnElement> Elements => Set<BpmnElement>();
    public DbSet<PropertyDefinition> PropertyDefinitions => Set<PropertyDefinition>();
    public DbSet<PropertyOption> PropertyOptions => Set<PropertyOption>();
    public DbSet<PropertyValue> PropertyValues => Set<PropertyValue>();
    public DbSet<ElementAttachment> ElementAttachments => Set<ElementAttachment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Process>(entity =>
        {
            entity.ToTable("processes");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ExternalKey).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(300).IsRequired();
            entity.HasIndex(x => x.ExternalKey).IsUnique();
        });

        modelBuilder.Entity<BpmnElement>(entity =>
        {
            entity.ToTable("bpmn_elements");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ExternalElementId).HasMaxLength(200).IsRequired();
            entity.Property(x => x.ElementType).HasMaxLength(200);
            entity.Property(x => x.Name).HasMaxLength(300);
            entity.HasIndex(x => new { x.ProcessId, x.ExternalElementId }).IsUnique();
            entity.HasOne<Process>().WithMany(x => x.Elements).HasForeignKey(x => x.ProcessId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(x => x.Attachments).WithOne().HasForeignKey(x => x.ElementId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ElementAttachment>(entity =>
        {
            entity.ToTable("element_attachments");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ObjectKey).HasMaxLength(500).IsRequired();
            entity.Property(x => x.FileName).HasMaxLength(300).IsRequired();
            entity.Property(x => x.ContentType).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Size).IsRequired();
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.HasIndex(x => x.ElementId);
        });

        modelBuilder.Entity<PropertyDefinition>(entity =>
        {
            entity.ToTable("property_definitions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Key).HasMaxLength(100).IsRequired();
            entity.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(1000);
            entity.Property(x => x.DataType).HasConversion<string>().HasMaxLength(40);
            entity.HasIndex(x => new { x.ProcessId, x.Key }).IsUnique();
            entity.Property(x => x.DefaultValueJson).HasColumnType("jsonb");
            entity.HasOne<Process>().WithMany(x => x.PropertyDefinitions).HasForeignKey(x => x.ProcessId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PropertyOption>(entity =>
        {
            entity.ToTable("property_options");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Value).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Label).HasMaxLength(200).IsRequired();
            entity.HasIndex(x => new { x.PropertyDefinitionId, x.Value }).IsUnique();
            entity.HasOne<PropertyDefinition>().WithMany(x => x.Options).HasForeignKey(x => x.PropertyDefinitionId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PropertyValue>(entity =>
        {
            entity.ToTable("property_values");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ValueJson).HasColumnType("jsonb").IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken().IsRequired();
            entity.HasIndex(x => new { x.ElementId, x.PropertyDefinitionId }).IsUnique();
            entity.HasOne<BpmnElement>().WithMany(x => x.PropertyValues).HasForeignKey(x => x.ElementId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<PropertyDefinition>().WithMany().HasForeignKey(x => x.PropertyDefinitionId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
