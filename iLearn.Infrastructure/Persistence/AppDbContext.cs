using iLearn.Application.Common;
using iLearn.Application.Interfaces.Services;
using iLearn.Domain.Common;
using iLearn.Domain.Entities;
using iLearn.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace iLearn.Infrastructure.Persistence
{
    public class AppDbContext : DbContext
    {
        private readonly IDateTime _dateTime;
        private readonly ICurrentUserService _currentUserService;

        public AppDbContext(
          DbContextOptions<AppDbContext> options,
          IDateTime dateTime,
          ICurrentUserService currentUserService)
          : base(options)
        {
            _dateTime = dateTime;
            _currentUserService = currentUserService;
        }

        public DbSet<Course> Courses { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }

        // --- เพิ่ม DbSet ใหม่ ---
        public DbSet<Category> Categories { get; set; }
        public DbSet<ContentItem> ContentItems { get; set; }
        public DbSet<CourseVersion> CourseVersions { get; set; }
        public DbSet<CourseContentItem> CourseContentItems { get; set; }
        public DbSet<FileStorage> FileStorages { get; set; }
        public DbSet<LearningLog> LearningLogs { get; set; }
        public DbSet<ScormRuntimeState> ScormRuntimeStates { get; set; }
        public DbSet<CourseType> CourseTypes { get; set; }
        public DbSet<AdminActivity> AdminActivities { get; set; }

        // DbSet เดิมที่มีอยู่แล้ว (ตรวจสอบว่ามีครบไหม)
        public DbSet<Division> Divisions { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<User> Users { get; set; } //User คือผู้ดูแลระบบ (Admin) ไม่ใช่นักเรียน ข้อมูลนักเรียนอยู่ใน LearnersController ซึ่งดึงมาจากระบบหลักผ่าน API
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<Assignment> Assignments { get; set; }

        // ── กลุ่มผู้เรียน ──
        public DbSet<LearnerGroup> LearnerGroups { get; set; }
        public DbSet<LearnerGroupMember> LearnerGroupMembers { get; set; }
        public DbSet<LearnerGroupCategory> LearnerGroupCategories { get; set; }

        // ── ตารางกลาง Enrollment <-> Assignment ──
        public DbSet<EnrollmentAssignment> EnrollmentAssignments { get; set; }

        // ── Notifications ──
        public DbSet<Notification> Notifications { get; set; }

        // ── Normalized Assignment → Course detail ──
        public DbSet<AssignmentCourse> AssignmentCourses { get; set; }

        // ── Read-only view for assignment list ──
        public DbSet<AssignmentListRow> AssignmentList { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ── Global Query Filter: ซ่อน soft-deleted records จากทุก query อัตโนมัติ ──
            ApplySoftDeleteFilters(modelBuilder);

            // 1. Config Enrollment (LearnerCode ไม่มี FK -> User)
            modelBuilder.Entity<Enrollment>()
                .HasOne(e => e.Course)
                .WithMany(c => c.Enrollments)
                .HasForeignKey(e => e.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            // Config EnrollmentAssignment (ตารางกลาง)
            modelBuilder.Entity<EnrollmentAssignment>()
                .HasOne(ea => ea.Enrollment)
                .WithMany(e => e.AssignmentLinks)
                .HasForeignKey(ea => ea.EnrollmentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<EnrollmentAssignment>()
                .HasOne(ea => ea.Assignment)
                .WithMany()
                .HasForeignKey(ea => ea.AssignmentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Unique: 1 Enrollment ต่อ 1 Assignment (ไม่ซ้ำ)
            modelBuilder.Entity<EnrollmentAssignment>()
                .HasIndex(ea => new { ea.EnrollmentId, ea.AssignmentId })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            // 2. Config CourseContentItem (Many-to-Many)
            modelBuilder.Entity<CourseContentItem>()
                   .HasKey(cr => cr.Id);
            modelBuilder.Entity<CourseContentItem>()
                .HasOne(cr => cr.CourseVersion)      // เปลี่ยนจาก Course เป็น CourseVersion
                .WithMany(cv => cv.CourseContentItems) // ต้องตรงกับ Property ใน CourseVersion
                .HasForeignKey(cr => cr.CourseVersionId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<CourseContentItem>()
                .HasOne(cr => cr.ContentItem)
                .WithMany(r => r.CourseContentItems)
                .HasForeignKey(cr => cr.ContentItemId)
                .OnDelete(DeleteBehavior.Restrict);
            // 3. Config ContentItem <-> FileStorage (1-to-1 or 1-to-Many)
            modelBuilder.Entity<ContentItem>()
                .HasOne(r => r.FileStorage)
                .WithOne() // หรือ WithMany ถ้าไฟล์เดียวใช้หลาย ContentItem
                .HasForeignKey<ContentItem>(r => r.FileStorageId);

            modelBuilder.Entity<FileStorage>()
                .Property(f => f.StoragePath)
                .HasMaxLength(500);

            modelBuilder.Entity<ScormRuntimeState>()
                .HasOne(state => state.Enrollment)
                .WithMany()
                .HasForeignKey(state => state.EnrollmentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ScormRuntimeState>()
                .HasOne(state => state.ContentItem)
                .WithMany()
                .HasForeignKey(state => state.ContentItemId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ScormRuntimeState>()
                .HasIndex(state => new { state.EnrollmentId, state.ContentItemId })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            modelBuilder.Entity<ScormRuntimeState>()
                .HasIndex(state => state.LastCommittedAtUtc);

            modelBuilder.Entity<ScormRuntimeState>()
                .Property(state => state.ScormVersion)
                .HasMaxLength(ScormRuntimeLimits.ScormVersionMaxLength);

            modelBuilder.Entity<ScormRuntimeState>()
                .Property(state => state.LessonLocation)
                .HasMaxLength(ScormRuntimeLimits.LessonLocationMaxLength);

            modelBuilder.Entity<ScormRuntimeState>()
                .Property(state => state.LessonStatus)
                .HasMaxLength(ScormRuntimeLimits.StatusMaxLength);

            modelBuilder.Entity<ScormRuntimeState>()
                .Property(state => state.CompletionStatus)
                .HasMaxLength(ScormRuntimeLimits.StatusMaxLength);

            modelBuilder.Entity<ScormRuntimeState>()
                .Property(state => state.SuccessStatus)
                .HasMaxLength(ScormRuntimeLimits.StatusMaxLength);

            modelBuilder.Entity<ScormRuntimeState>()
                .Property(state => state.SessionTime)
                .HasMaxLength(ScormRuntimeLimits.SessionTimeMaxLength);

            modelBuilder.Entity<ScormRuntimeState>()
                .Property(state => state.TotalTime)
                .HasMaxLength(ScormRuntimeLimits.TotalTimeMaxLength);

            modelBuilder.Entity<ScormRuntimeState>()
                .Property(state => state.Entry)
                .HasMaxLength(ScormRuntimeLimits.EntryMaxLength);

            modelBuilder.Entity<ScormRuntimeState>()
                .Property(state => state.Exit)
                .HasMaxLength(ScormRuntimeLimits.ExitMaxLength);

            modelBuilder.Entity<ScormRuntimeState>()
                .Property(state => state.RawScore)
                .HasPrecision(7, 2);

            modelBuilder.Entity<CourseVersion>()
                .HasOne(cv => cv.Course)
                .WithMany(c => c.Versions)
                .HasForeignKey(cv => cv.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            // Config LearnerGroup <-> LearnerGroupMember
            modelBuilder.Entity<LearnerGroupMember>()
                .HasOne(m => m.LearnerGroup)
                .WithMany(g => g.Members)
                .HasForeignKey(m => m.LearnerGroupId)
                .OnDelete(DeleteBehavior.Cascade);

            // Config LearnerGroup -> LearnerGroupCategory (folder)
            modelBuilder.Entity<LearnerGroup>()
                .HasOne(g => g.Category)
                .WithMany(c => c.LearnerGroups)
                .HasForeignKey(g => g.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LearnerGroup>()
                .HasIndex(g => g.CategoryId);

            // Config LearnerGroupCategory self-reference (tree hierarchy)
            modelBuilder.Entity<LearnerGroupCategory>()
                .HasOne(c => c.Parent)
                .WithMany(c => c.Children)
                .HasForeignKey(c => c.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LearnerGroupCategory>()
                .HasIndex(c => c.ParentId);

            modelBuilder.Entity<LearnerGroupCategory>()
                .HasIndex(c => c.Path);

            modelBuilder.Entity<LearnerGroupCategory>()
                .Property(c => c.Path)
                .HasMaxLength(450);

            // Config AssignmentCourse (normalized detail)
            modelBuilder.Entity<AssignmentCourse>()
                .HasOne(ac => ac.Assignment)
                .WithMany(a => a.AssignmentCourses)
                .HasForeignKey(ac => ac.AssignmentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AssignmentCourse>()
                .HasOne(ac => ac.Course)
                .WithMany(c => c.AssignmentCourses)
                .HasForeignKey(ac => ac.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            // Unique: 1 Assignment + 1 Course (ไม่ซ้ำ)
            modelBuilder.Entity<AssignmentCourse>()
                .HasIndex(ac => new { ac.AssignmentId, ac.CourseId })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            // ── Read-only view: vw_AssignmentList (keyless — no soft-delete filter needed) ──
            modelBuilder.Entity<AssignmentListRow>(entity =>
            {
                entity.HasNoKey();
                entity.ToView("vw_AssignmentList");
            });

            // ── DB Sequence for AssignmentNo running number ──
            modelBuilder.HasSequence<int>("AssignmentNoSeq")
                .StartsAt(1)
                .IncrementsBy(1);

            // Config Assignment <-> LearnerGroup
            modelBuilder.Entity<Assignment>()
                .HasOne(a => a.LearnerGroup)
                .WithMany(g => g.Assignments)
                .HasForeignKey(a => a.LearnerGroupId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Assignment>()
                .Property(a => a.AssignmentNo)
                .HasMaxLength(32);

            modelBuilder.Entity<Assignment>()
                .HasIndex(a => new { a.AssignmentNo, a.CourseId })
                .IsUnique()
                .HasFilter("[AssignmentNo] IS NOT NULL AND [CourseId] IS NOT NULL AND [IsDeleted] = 0");

            // Config Course <-> CourseType
            modelBuilder.Entity<Course>()
                .Property(c => c.Status)
                .HasConversion<int>()
                .HasDefaultValue(CourseStatus.Draft);

            modelBuilder.Entity<Course>()
                .HasOne(c => c.CourseType)
                .WithMany(ct => ct.Courses)
                .HasForeignKey(c => c.CourseTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Seed ข้อมูล CourseType เริ่มต้น (จาก enum เดิม: Special=0 -> Id=1, General=1 -> Id=2)
            modelBuilder.Entity<CourseType>().HasData(
                new CourseType { Id = 1, Name = "Special", Description = "วิชาเฉพาะทาง (Rule-based)", IsActive = true, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new CourseType { Id = 2, Name = "General", Description = "วิชาทั่วไป (Auto-assign)", IsActive = true, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
            );

            // ── Notification ──
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.Property(n => n.RecipientUserId).IsRequired().HasMaxLength(100);
                entity.Property(n => n.Type).IsRequired().HasMaxLength(50);
                entity.Property(n => n.Level).IsRequired().HasMaxLength(50);
                entity.Property(n => n.Title).IsRequired().HasMaxLength(200);
                entity.Property(n => n.Message).HasMaxLength(1000);
                entity.Property(n => n.LinkPath).HasMaxLength(300);
                entity.Property(n => n.EntityType).HasMaxLength(100);

                entity.HasIndex(n => new { n.RecipientUserId, n.IsRead, n.CreatedAt })
                    .IsDescending(false, false, true)
                    .HasDatabaseName("IX_Notifications_Recipient_Read_CreatedDesc");
            });
        }

        public override int SaveChanges()
        {
            SetAuditFields();
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SetAuditFields();
            return await base.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// วน loop ทุก Entity ที่สืบทอด BaseEntity แล้วใส่ HasQueryFilter(e => !e.IsDeleted) อัตโนมัติ
        /// </summary>
        private static void ApplySoftDeleteFilters(ModelBuilder modelBuilder)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (!typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
                    continue;

                var param     = Expression.Parameter(entityType.ClrType, "e");
                var property  = Expression.Property(param, nameof(BaseEntity.IsDeleted));
                var condition = Expression.Lambda(Expression.Not(property), param);

                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(condition);
            }
        }

        private void SetAuditFields()
        {
            var entries = ChangeTracker.Entries<BaseEntity>();
            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedAt = _dateTime.Now;
                    entry.Entity.CreatedBy = _currentUserService.UserId;
                }
                else if (entry.State == EntityState.Modified)
                {
                    // ถ้าเป็น Soft Delete ให้บันทึก DeletedBy แทน UpdatedBy
                    if (entry.Entity.IsDeleted && entry.Entity.DeletedBy == null)
                    {
                        entry.Entity.DeletedBy = _currentUserService.UserId;
                    }
                    else
                    {
                        entry.Entity.UpdatedAt = _dateTime.Now;
                        entry.Entity.UpdatedBy = _currentUserService.UserId;
                    }

                    // 🛡️ ป้องกันไม่ให้ CreatedAt และ CreatedBy ถูกแก้ไขโดยไม่ตั้งใจตอน Update
                    entry.Property(x => x.CreatedAt).IsModified = false;
                    entry.Property(x => x.CreatedBy).IsModified = false;
                }
            }
        }
    }
}