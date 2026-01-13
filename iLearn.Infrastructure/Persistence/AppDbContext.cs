using iLearn.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace iLearn.Infrastructure.Persistence
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Course> Courses { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }
        // public DbSet<AssignmentRule> AssignmentRules { get; set; } // อย่าลืมสร้างไฟล์ AssignmentRule.cs ใน Domain ด้วยนะครับ

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // --- Config Enrollment (ความสัมพันธ์ User <-> Course) ---
            modelBuilder.Entity<Enrollment>()
                .HasOne(e => e.Course)
                .WithMany(c => c.Enrollments)
                .HasForeignKey(e => e.CourseId)
                .OnDelete(DeleteBehavior.Restrict); // ป้องกันการลบคอร์สแล้วกระทบประวัติการเรียน

            // ถ้ามี User Entity
            modelBuilder.Entity<Enrollment>()
                .HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // --- Config AssignmentRule (ถ้ามี) ---
            /*
            modelBuilder.Entity<AssignmentRule>()
                .HasOne(ar => ar.Course)
                .WithMany(c => c.AssignmentRules)
                .HasForeignKey(ar => ar.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
            */
        }
    }
}