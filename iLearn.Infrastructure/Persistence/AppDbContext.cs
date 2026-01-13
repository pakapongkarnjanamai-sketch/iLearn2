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

        // --- เพิ่ม DbSet ใหม่ ---
        public DbSet<Category> Categories { get; set; }
        public DbSet<Resource> Resources { get; set; }
        public DbSet<CourseResource> CourseResources { get; set; }
        public DbSet<FileStorage> FileStorages { get; set; }
        public DbSet<LearningLog> LearningLogs { get; set; }

        // DbSet เดิมที่มีอยู่แล้ว (ตรวจสอบว่ามีครบไหม)
        public DbSet<Division> Divisions { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<AssignmentRule> AssignmentRules { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. Config Enrollment (StudentCode ไม่มี FK -> User)
            modelBuilder.Entity<Enrollment>()
                .HasOne(e => e.Course)
                .WithMany(c => c.Enrollments)
                .HasForeignKey(e => e.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            // 2. Config CourseResource (Many-to-Many)
            modelBuilder.Entity<CourseResource>()
                .HasKey(cr => cr.Id); // หรือจะใช้ Composite Key ก็ได้

            modelBuilder.Entity<CourseResource>()
                .HasOne(cr => cr.Course)
                .WithMany(c => c.CourseResources) // ต้องไปเพิ่ม Property นี้ใน Course.cs ด้วย
                .HasForeignKey(cr => cr.CourseId);

            modelBuilder.Entity<CourseResource>()
                .HasOne(cr => cr.Resource)
                .WithMany(r => r.CourseResources)
                .HasForeignKey(cr => cr.ResourceId);

            // 3. Config Resource <-> FileStorage (1-to-1 or 1-to-Many)
            modelBuilder.Entity<Resource>()
                .HasOne(r => r.FileStorage)
                .WithOne() // หรือ WithMany ถ้าไฟล์เดียวใช้หลาย Resource
                .HasForeignKey<Resource>(r => r.FileStorageId);
        }
    }
}