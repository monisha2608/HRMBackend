using HRM.Backend.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HRM.Backend.Data
{
    public class ApplicationDbContext : IdentityDbContext<AppUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Job> Jobs => Set<Job>();
        public DbSet<Application> Applications => Set<Application>();
        public DbSet<ApplicationStatusHistory> ApplicationStatusHistories => Set<ApplicationStatusHistory>();
        public DbSet<ApplicationNote> ApplicationNotes => Set<ApplicationNote>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity(JobEntity());

            builder.Entity(ApplicationEntity());
        }

        private static Action<Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<Job>> JobEntity()
            => entity =>
            {
                entity.HasOne(j => j.PostedByUser)
                      .WithMany()
                      .HasForeignKey(j => j.PostedByUserId)
                      .OnDelete(DeleteBehavior.Restrict);
            };

        private static Action<Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<Application>> ApplicationEntity()
            => entity =>
            {
                entity.HasOne(a => a.Job)
                      .WithMany()
                      .HasForeignKey(a => a.JobId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(a => a.CandidateUser)
                      .WithMany()
                      .HasForeignKey(a => a.CandidateUserId)
                      .OnDelete(DeleteBehavior.Restrict);
            };


    }
}
