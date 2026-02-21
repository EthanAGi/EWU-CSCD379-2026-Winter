using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MortuaryAssistant.Api.Models;

namespace MortuaryAssistant.Api.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // Existing
    public DbSet<CaseFile> CaseFiles => Set<CaseFile>();

    // New tables
    public DbSet<Decedent> Decedents => Set<Decedent>();
    public DbSet<WorkflowStepTemplate> WorkflowStepTemplates => Set<WorkflowStepTemplate>();
    public DbSet<CaseTask> CaseTasks => Set<CaseTask>();
    public DbSet<CaseNote> CaseNotes => Set<CaseNote>();

    public DbSet<Equipment> Equipment => Set<Equipment>();
    public DbSet<EquipmentCheckout> EquipmentCheckouts => Set<EquipmentCheckout>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ✅ CaseNumber should be unique (common in LOB apps)
        builder.Entity<CaseFile>()
            .HasIndex(c => c.CaseNumber)
            .IsUnique();

        // ✅ Decedent 1:1 CaseFile (Cascade delete ok)
        builder.Entity<CaseFile>()
            .HasOne(c => c.Decedent)
            .WithOne(d => d.CaseFile)
            .HasForeignKey<Decedent>(d => d.CaseFileId)
            .OnDelete(DeleteBehavior.Cascade);

        // ✅ CaseTasks (CaseFile 1:N)
        builder.Entity<CaseTask>()
            .HasOne(t => t.CaseFile)
            .WithMany(c => c.Tasks)
            .HasForeignKey(t => t.CaseFileId)
            .OnDelete(DeleteBehavior.Cascade);

        // ✅ CaseTasks -> WorkflowStepTemplate
        builder.Entity<CaseTask>()
            .HasOne(t => t.WorkflowStepTemplate)
            .WithMany()
            .HasForeignKey(t => t.WorkflowStepTemplateId)
            .OnDelete(DeleteBehavior.Restrict);

        // ✅ CaseTasks -> Assigned user (optional)
        builder.Entity<CaseTask>()
            .HasOne(t => t.AssignedToUser)
            .WithMany()
            .HasForeignKey(t => t.AssignedToUserId)
            .OnDelete(DeleteBehavior.SetNull);

        // ✅ CaseNotes (CaseFile 1:N)
        builder.Entity<CaseNote>()
            .HasOne(n => n.CaseFile)
            .WithMany(c => c.Notes)
            .HasForeignKey(n => n.CaseFileId)
            .OnDelete(DeleteBehavior.Cascade);

        // ✅ EquipmentCheckout -> Equipment
        builder.Entity<EquipmentCheckout>()
            .HasOne(ec => ec.Equipment)
            .WithMany()
            .HasForeignKey(ec => ec.EquipmentId)
            .OnDelete(DeleteBehavior.Restrict);

        // ✅ EquipmentCheckout -> CaseFile (optional)
        builder.Entity<EquipmentCheckout>()
            .HasOne(ec => ec.CaseFile)
            .WithMany(c => c.EquipmentCheckouts)
            .HasForeignKey(ec => ec.CaseFileId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}