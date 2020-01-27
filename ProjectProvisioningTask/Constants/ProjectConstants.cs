namespace ProjectProvisioningTask.Constants
{
    public static class ProjectConstants
    {
        public const string ProjectListTitle = "Projects";

        public const string AuditListTitle = "Provisioning Infos";

        public static class Project
        {
            public const string Title = "Title";
            public const string Description = "Description";
            public const string Owners = "Owners";
            public const string Members = "Members";
            public const string Visitors = "Visitors";
            public const string Status = "Project Status";
            public const string Category = "Category";
            public const string Address = "Address";
        }

        public static class ProjectStatus
        {
            public const string InProvisioning = "In Provisioning";
            public const string Active = "Active";
            public const string Closed = "Closed";
        }

        public static class ProjectCategory
        {
            public const string Development = "Development";
            public const string Education = "Education ";
            public const string Entertainment = "Entertainment";
        }

        public static class Lists
        {
            public const string Tasks = "Project Tasks";
            public const string Documents = "Project Documents";
            public const string Notes = "Project Notes";
        }

        public static class ProjectDocumentsFields
        {
            public const string Title = "Project Title";
            public const string Description = "Project Description";
            public const string ProjectAddress = "Project Address";
            public const string ProjectCategory = "Project Category";
        }

        public static class ProjectAuditFields
        {
            public const string User = "User";
            public const string Url = "Project Site URL";
            public const string Action = "Action";
        }

    }
}