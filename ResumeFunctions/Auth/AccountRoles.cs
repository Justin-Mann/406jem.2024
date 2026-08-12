namespace ResumeFunctions.Auth
{
    public static class AccountRoles
    {
        public const string Visitor = "visitor";

        // Same wire value as the original "Admin" role from #25 — renamed at the C# level
        // only, so the already-seeded production account and any live JWTs keep working
        // without a data/token migration. Semantically this is now "Resume Admin": can CRUD
        // only their own resumes/project listings.
        public const string ResumeAdmin = "admin";

        // New top role introduced in #28: can CRUD any owner's resumes/project listings and
        // is the only role that can change SiteConfig (what's actually public).
        public const string SuperAdmin = "superadmin";

        /// <summary>Shared by #45's resume-poster directory (listing and contact-relay) so the
        /// "who counts as a poster" rule lives in one place rather than two API classes.</summary>
        public static bool IsResumePosterRole(string role) => role == ResumeAdmin || role == SuperAdmin;
    }
}
