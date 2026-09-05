using CareerPlatform.Api.Features.Content.Domain;
using CareerPlatform.Api.Features.Courses.Domain;
using CareerPlatform.Api.Features.Mentorship.Domain;
using CareerPlatform.Api.Features.Broadcasts.Domain;
using CareerPlatform.Api.Features.Meetings.Domain;
using CareerPlatform.Api.Features.Assessments.Domain;
using CareerPlatform.Api.Features.LearningPaths.Domain;
using CareerPlatform.Api.Features.PlacementCompanies.Domain;
using CareerPlatform.Api.Features.Interviews.Domain;
using CareerPlatform.Api.Features.Practice.Domain;
using CareerPlatform.Api.Features.Resumes.Domain;
using CareerPlatform.Api.Features.Notifications.Domain;
using CareerPlatform.Api.Features.Offers.Domain;
using CareerPlatform.Api.Features.Orders.Domain;
using CareerPlatform.Api.Features.Payments.Domain;
using CareerPlatform.Api.Features.Plans.Domain;
using CareerPlatform.Api.Features.Settings.Domain;
using CareerPlatform.Api.Features.Students.Domain;
using CareerPlatform.Api.Features.SubscriptionPlans.Domain;
using CareerPlatform.Api.Features.Users.Domain;
using Microsoft.EntityFrameworkCore;

namespace CareerPlatform.Api.Infrastructure.Persistence;

/// <summary>
/// The application's EF Core database context. Reproduces the legacy
/// <c>ApplicationDbContext</c> DbSets verbatim (unchanged names) so table/column/index shapes are
/// preserved through the namespace rename (Req 17.7, 18.1, 18.2, 24.5). All model configuration
/// lives in per-entity <see cref="IEntityTypeConfiguration{TEntity}"/> classes applied via
/// <c>ApplyConfigurationsFromAssembly</c>.
/// </summary>
public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<PendingRegistration> PendingRegistrations => Set<PendingRegistration>();
    public DbSet<CourseContent> CourseContents => Set<CourseContent>();
    public DbSet<ProgressLog> ProgressLogs => Set<ProgressLog>();
    public DbSet<OfferLetter> OfferLetters => Set<OfferLetter>();
    public DbSet<Transaction> Transactions => Set<Transaction>();

    // Dynamic Tutorial Content
    public DbSet<Language> Languages => Set<Language>();
    public DbSet<Section> Sections => Set<Section>();
    public DbSet<Topic> Topics => Set<Topic>();

    // Mentorship & 1:1 Meetings
    public DbSet<Mentor> Mentors => Set<Mentor>();
    public DbSet<MentorSlot> MentorSlots => Set<MentorSlot>();
    public DbSet<MeetingBooking> MeetingBookings => Set<MeetingBooking>();
    public DbSet<MentorReview> MentorReviews => Set<MentorReview>();

    // Purchasable catalog
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Plan> Plans => Set<Plan>();

    // Orders & Enrollments
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();

    // Notifications
    public DbSet<Notification> Notifications => Set<Notification>();

    // Admin broadcast audit trail (fan-out source for Notifications)
    public DbSet<Broadcast> Broadcasts => Set<Broadcast>();

    // Admin-scheduled meetings (1:1 and cohort webinars) — distinct from MeetingBookings
    public DbSet<Meeting> Meetings => Set<Meeting>();

    // Practice questions (public /practice + admin CRUD) + per-student bookmarks
    public DbSet<PracticeQuestion> PracticeQuestions => Set<PracticeQuestion>();
    public DbSet<CareerPlatform.Api.Features.Practice.Domain.PracticeBookmark> PracticeBookmarks =>
        Set<CareerPlatform.Api.Features.Practice.Domain.PracticeBookmark>();

    // Placement partner companies (public /placement + admin CRUD)
    public DbSet<PlacementCompany> PlacementCompanies => Set<PlacementCompany>();

    // Curated learning paths (public /learning-paths + admin CRUD)
    public DbSet<LearningPath> LearningPaths => Set<LearningPath>();

    // Practice tests / assessments (student catalog + admin CRUD) + attempts
    public DbSet<Assessment> Assessments => Set<Assessment>();
    public DbSet<CareerPlatform.Api.Features.Assessments.Domain.AssessmentAttempt> AssessmentAttempts =>
        Set<CareerPlatform.Api.Features.Assessments.Domain.AssessmentAttempt>();
    // Structured question bank + graders for the coding-assessment runner.
    public DbSet<CareerPlatform.Api.Features.Assessments.Domain.AssessmentQuestion> AssessmentQuestions =>
        Set<CareerPlatform.Api.Features.Assessments.Domain.AssessmentQuestion>();
    public DbSet<CareerPlatform.Api.Features.Assessments.Domain.AssessmentTestCase> AssessmentTestCases =>
        Set<CareerPlatform.Api.Features.Assessments.Domain.AssessmentTestCase>();
    public DbSet<CareerPlatform.Api.Features.Assessments.Domain.AssessmentAttemptAnswer> AssessmentAttemptAnswers =>
        Set<CareerPlatform.Api.Features.Assessments.Domain.AssessmentAttemptAnswer>();

    // Resume templates (admin CRUD) + per-student submissions + PDF uploads
    public DbSet<ResumeTemplate> ResumeTemplates => Set<ResumeTemplate>();
    public DbSet<ResumeSubmission> ResumeSubmissions => Set<ResumeSubmission>();
    public DbSet<StudentResumeUpload> StudentResumeUploads => Set<StudentResumeUpload>();
    public DbSet<CareerPlatform.Api.Features.Resumes.Domain.ResumeDraft> ResumeDrafts =>
        Set<CareerPlatform.Api.Features.Resumes.Domain.ResumeDraft>();

    // Interview question bank (public + admin CRUD) + per-student mock sessions
    public DbSet<InterviewQuestion> InterviewQuestions => Set<InterviewQuestion>();
    public DbSet<MockInterviewSession> MockInterviewSessions => Set<MockInterviewSession>();

    // Certificates (per-student, admin-issued)
    public DbSet<CareerPlatform.Api.Features.Certificates.Domain.Certificate> Certificates =>
        Set<CareerPlatform.Api.Features.Certificates.Domain.Certificate>();

    // Support tickets (student-raised, admin-triaged)
    public DbSet<CareerPlatform.Api.Features.Support.Domain.SupportTicket> SupportTickets =>
        Set<CareerPlatform.Api.Features.Support.Domain.SupportTicket>();
    public DbSet<CareerPlatform.Api.Features.Support.Domain.SupportTicketMessage> SupportTicketMessages =>
        Set<CareerPlatform.Api.Features.Support.Domain.SupportTicketMessage>();

    // Dynamic subscription pricing
    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();

    // Admin-managed platform configuration (dynamic key/value settings)
    public DbSet<PlatformSetting> PlatformSettings => Set<PlatformSetting>();

    // Learning progress (per-student, polymorphic on ResourceType)
    public DbSet<CareerPlatform.Api.Features.Learning.Domain.LearningProgress> LearningProgress =>
        Set<CareerPlatform.Api.Features.Learning.Domain.LearningProgress>();

    // Phase 5 admin-surface backfill
    public DbSet<CareerPlatform.Api.Features.Coupons.Domain.Coupon> Coupons =>
        Set<CareerPlatform.Api.Features.Coupons.Domain.Coupon>();
    public DbSet<CareerPlatform.Api.Features.Reviews.Domain.CourseReview> CourseReviews =>
        Set<CareerPlatform.Api.Features.Reviews.Domain.CourseReview>();
    public DbSet<CareerPlatform.Api.Features.Cms.Domain.CmsFaq> CmsFaqs =>
        Set<CareerPlatform.Api.Features.Cms.Domain.CmsFaq>();
    public DbSet<CareerPlatform.Api.Features.Cms.Domain.CmsTestimonial> CmsTestimonials =>
        Set<CareerPlatform.Api.Features.Cms.Domain.CmsTestimonial>();
    public DbSet<CareerPlatform.Api.Features.Cms.Domain.CmsNavigationLink> CmsNavigationLinks =>
        Set<CareerPlatform.Api.Features.Cms.Domain.CmsNavigationLink>();
    public DbSet<CareerPlatform.Api.Features.Skills.Domain.UserSkill> UserSkills =>
        Set<CareerPlatform.Api.Features.Skills.Domain.UserSkill>();

    // Student self-service profile (qualifications + notification/visibility preferences)
    public DbSet<CareerPlatform.Api.Features.StudentProfile.Domain.StudentEducation> StudentEducations =>
        Set<CareerPlatform.Api.Features.StudentProfile.Domain.StudentEducation>();
    public DbSet<CareerPlatform.Api.Features.StudentProfile.Domain.StudentPreferences> StudentPreferences =>
        Set<CareerPlatform.Api.Features.StudentProfile.Domain.StudentPreferences>();
    public DbSet<CareerPlatform.Api.Features.CourseCategories.Domain.CourseCategory> CourseCategories =>
        Set<CareerPlatform.Api.Features.CourseCategories.Domain.CourseCategory>();
    public DbSet<CareerPlatform.Api.Features.PlacementRoles.Domain.PlacementRole> PlacementRoles =>
        Set<CareerPlatform.Api.Features.PlacementRoles.Domain.PlacementRole>();
    public DbSet<CareerPlatform.Api.Features.Products.Domain.Product> Products =>
        Set<CareerPlatform.Api.Features.Products.Domain.Product>();
    public DbSet<CareerPlatform.Api.Features.AdminLedger.Domain.OrderInvoice> OrderInvoices =>
        Set<CareerPlatform.Api.Features.AdminLedger.Domain.OrderInvoice>();
    public DbSet<CareerPlatform.Api.Features.AdminLedger.Domain.AdminAuditLog> AdminAuditLogs =>
        Set<CareerPlatform.Api.Features.AdminLedger.Domain.AdminAuditLog>();
    public DbSet<CareerPlatform.Api.Features.CourseLessons.Domain.CourseLesson> CourseLessons =>
        Set<CareerPlatform.Api.Features.CourseLessons.Domain.CourseLesson>();
    public DbSet<CareerPlatform.Api.Features.PracticeBanks.Domain.PracticeQuestionBank> PracticeQuestionBanks =>
        Set<CareerPlatform.Api.Features.PracticeBanks.Domain.PracticeQuestionBank>();
    public DbSet<CareerPlatform.Api.Features.PracticeBanks.Domain.PracticeQuestionBankItem> PracticeQuestionBankItems =>
        Set<CareerPlatform.Api.Features.PracticeBanks.Domain.PracticeQuestionBankItem>();
    public DbSet<CareerPlatform.Api.Features.MentorAssignments.Domain.MentorAssignment> MentorAssignments =>
        Set<CareerPlatform.Api.Features.MentorAssignments.Domain.MentorAssignment>();

    // Admin-surface backfill (awaiting-backend features)
    public DbSet<CareerPlatform.Api.Features.PlacementPlans.Domain.PlacementPlan> PlacementPlans =>
        Set<CareerPlatform.Api.Features.PlacementPlans.Domain.PlacementPlan>();
    public DbSet<CareerPlatform.Api.Features.MentorshipPlans.Domain.MentorshipPlan> MentorshipPlans =>
        Set<CareerPlatform.Api.Features.MentorshipPlans.Domain.MentorshipPlan>();
    public DbSet<CareerPlatform.Api.Features.Cms.Domain.CmsBanner> CmsBanners =>
        Set<CareerPlatform.Api.Features.Cms.Domain.CmsBanner>();
    public DbSet<CareerPlatform.Api.Features.Cms.Domain.CmsHomepageConfig> CmsHomepageConfigs =>
        Set<CareerPlatform.Api.Features.Cms.Domain.CmsHomepageConfig>();
    public DbSet<CareerPlatform.Api.Features.Interviews.Domain.InterviewRubric> InterviewRubrics =>
        Set<CareerPlatform.Api.Features.Interviews.Domain.InterviewRubric>();

    // User-authored articles (write-an-article workflow: draft → in_review → published)
    public DbSet<CareerPlatform.Api.Features.Posts.Domain.Post> Posts =>
        Set<CareerPlatform.Api.Features.Posts.Domain.Post>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Per-entity IEntityTypeConfiguration<T> classes reproduce the legacy OnModelCreating
        // rules verbatim (keys, ValueGeneratedNever, unique/indexed columns, relationships) so
        // table/column/index shapes remain byte-identical (Req 18.1, 18.2, 24.5).
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
