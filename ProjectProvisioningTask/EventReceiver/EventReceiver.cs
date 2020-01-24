using System;
using System.Security.Permissions;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Utilities;
using Microsoft.SharePoint.Workflow;
using System.Linq;
using System.Collections.Generic;
using static ProjectProvisioningTask.EventReceiver.ProjectStringConstants;
using Microsoft.SharePoint.Administration;

namespace ProjectProvisioningTask.EventReceiver
{
    public static class ProjectStringConstants
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

    public class ULSLogger : ILogger
    {
        SPDiagnosticsService _service;
        public ULSLogger()
        {
            _service = SPDiagnosticsService.Local;
        }

        void Log(TraceSeverity traceSeverity, EventSeverity eventSeverity, string message)
        {
            _service.WriteTrace(0, new SPDiagnosticsCategory("My Category", traceSeverity, eventSeverity), traceSeverity, message);
        }

        public void LogError(string message)
        {
            Log(TraceSeverity.Unexpected, EventSeverity.Error, message);
        }

        public void LogInformation(string message)
        {
            Log(TraceSeverity.Verbose, EventSeverity.Information, message);
        }

        public void LogWarning(string message)
        {
            Log(TraceSeverity.Medium, EventSeverity.Warning, message);
        }
    }

    public interface ILogger
    {
        void LogError(string message);
        void LogWarning(string message);
        void LogInformation(string message);
    }

    /// <summary>
    /// List Item Events
    /// </summary>
    public class EventReceiver : SPItemEventReceiver
    {
        /// <summary>
        /// An _item was added
        /// </summary>
        public override void ItemAdded(SPItemEventProperties properties)
        {
            base.ItemAdded(properties);

            if (properties.ListTitle != ProjectListTitle)
            {
                return;
            }

            ILogger logger = new ULSLogger();

            var audit = new ProvisionAudit(properties.Web);


            var worker = new ProvisionWorker(properties.Web, properties.ListItem);
            worker.ProvisionStarted += new ProvisionWorker.ProvisionStartedEventHandler((sender, args) =>
            {
                logger.LogInformation(args.Action);

            });

            worker.DoWork();
        }
    }

    public class ProvisionAudit
    {
        SPWeb _web;
        SPList _list;

        public ProvisionAudit(SPWeb web)
        {
            _web = web;
            CreateAuditList();
        }

        public void Audit(string user, string url, string action)
        {
            var item= _list.AddItem();
            item[ProjectAuditFields.User] = user;
            item[ProjectAuditFields.Url] = url;
            item[ProjectAuditFields.Action] = action;
            item.Update();
        }

        private void CreateAuditList()
        {
            var guidAuditList = _web.Lists.Add(AuditListTitle, null, SPListTemplateType.GenericList);
            _list = _web.Lists[guidAuditList];

            var userField = _list.Fields.CreateNewField(SPFieldType.Text.ToString(), ProjectAuditFields.User);
            userField.Required = false;
            userField.ShowInViewForms = true;
            _list.Fields.Add(userField);

            var urlField = _list.Fields.CreateNewField(SPFieldType.Text.ToString(), ProjectAuditFields.Url);
            userField.Required = false;
            userField.ShowInViewForms = true;
            _list.Fields.Add(urlField);

            var actionField = _list.Fields.CreateNewField(SPFieldType.Text.ToString(), ProjectAuditFields.Url);
            userField.Required = false;
            userField.ShowInViewForms = true;
            _list.Fields.Add(actionField);

            _list.Fields.Add(userField);
            _list.Fields.Add(urlField);
            _list.Fields.Add(actionField);
        }
    }

    public class ProvisionWorker
    {
        SPWeb _web;
        SPListItem _item;

        public delegate void ProvisionStartedEventHandler(object sender, ProvisionStartedEventArgs e);
        public delegate void ProvisionCompletedEventHandler(object sender, ProvisionCompletedEventArgs e);
        public delegate void SubWebCreatingEventHandler(object sender, SubWebCreatingEventArgs e);

        public event ProvisionStartedEventHandler ProvisionStarted;
        public event ProvisionCompletedEventHandler ProvisionCompleted;
        public event SubWebCreatingEventHandler SubWebCreating;

        public ProvisionWorker(SPWeb web, SPListItem item)
        {
            web = _web;
            item = _item;
        }

        public void DoWork()
        {
            ProvisionStarted.Invoke(this, new ProvisionStartedEventArgs());

            if (_web.Webs.Any(x => x.Title == _item.Title))
            {
                ProvisionCompleted?.Invoke(this, new ProvisionCompletedEventArgs());
                return;
            }

            var owners = _item[Project.Owners] as SPFieldUserValueCollection;
            var members = _item[Project.Members] as SPFieldUserValueCollection;
            var visitors = _item[Project.Visitors] as SPFieldUserValueCollection;

            if (!owners.Any(x => x.User.ID == _web.CurrentUser.ID))
            {
                ProvisionCompleted?.Invoke(this, new ProvisionCompletedEventArgs());
                return;
            }

            SubWebCreating.Invoke(this, new SubWebCreatingEventArgs());

            using (var subweb = _web.Webs.Add(
                _item.Title.ToLower(),
                _item.Title, _item[Project.Description] as string,
                _web.Language,
                SPWebTemplate.WebTemplateSTS,
                false,
                false
                ))
            {
                _item[Project.Status] = ProjectStatus.InProvisioning;

                CreateSiteGroups(subweb, owners, members, visitors);

                CreateLists(subweb);

                _item[Project.Status] = ProjectStatus.Active;

                _item.Update();
            }

            ProvisionCompleted?.Invoke(this, new ProvisionCompletedEventArgs());
        }

        private void CreateLists(SPWeb subweb)
        {
            subweb.Lists.Add(Lists.Tasks, null, SPListTemplateType.Tasks);
            var guidDocs = subweb.Lists.Add(Lists.Documents, null, SPListTemplateType.DocumentLibrary);
            subweb.Lists.Add(Lists.Notes, null, SPListTemplateType.GenericList);

            var docList = subweb.Lists[guidDocs];

            AddDocLibFields(docList);

            docList.Update();
        }

        private void AddDocLibFields(SPList docList)
        {
            var titleField = docList.Fields.CreateNewField(SPFieldType.Text.ToString(), ProjectDocumentsFields.Title);

            titleField.Required = true;
            titleField.DefaultValue = _item.Title;

            var descriptionField = docList.Fields.CreateNewField(SPFieldType.Note.ToString(), ProjectDocumentsFields.Description);
            descriptionField.Required = false;
            descriptionField.DefaultValue = _item[Project.Description] as string;
            descriptionField.ShowInViewForms = true;

            var addressField = docList.Fields.CreateNewField(SPFieldType.Note.ToString(), ProjectDocumentsFields.ProjectAddress);
            addressField.Required = false;
            addressField.DefaultValue = _item[Project.Address] as string;
            addressField.ShowInViewForms = true;
            docList.Fields.Add(addressField);

            var categoryField = docList.Fields.CreateNewField(SPFieldType.Choice.ToString(), ProjectDocumentsFields.ProjectCategory);
            categoryField.Required = true;
            categoryField.DefaultValue = _item[Project.Category] as string;
            categoryField.ShowInViewForms = true;
            categoryField.ShowInEditForm = false;

            docList.Fields.Add(titleField);
            docList.Fields.Add(descriptionField);
            docList.Fields.Add(categoryField);
        }

        private void CreateSiteGroups(SPWeb subweb, SPFieldUserValueCollection owners, SPFieldUserValueCollection members, SPFieldUserValueCollection visitors)
        {
            subweb.BreakRoleInheritance(false, false);

            var strOwners = $"{_item.Title} {Project.Owners}";
            var strMembers = $"{_item.Title} {Project.Members}";
            var strVisitors = $"{_item.Title} {Project.Visitors}";

            subweb.SiteGroups.Add(strOwners, subweb.CurrentUser, null, null);
            subweb.SiteGroups.Add(strMembers, subweb.CurrentUser, null, null);
            subweb.SiteGroups.Add(strVisitors, subweb.CurrentUser, null, null);

            Func<SPFieldUserValue, SPUserInfo> del = x => new SPUserInfo()
            {
                Name = x.User.Name,
                Email = x.User.Email,
                LoginName = x.User.LoginName,
                Notes = x.User.Notes
            };

            subweb.SiteGroups[strMembers].Users.AddCollection(owners?.Select(del)?.ToArray());
            subweb.SiteGroups[strMembers].Users.AddCollection(members?.Select(del)?.ToArray());
            subweb.SiteGroups[strVisitors].Users.AddCollection(visitors?.Select(del)?.ToArray());
        }
    }

    public class ProvisionEventArgs : EventArgs
    {
        public SPUser User { get; }
        public string Url { get; }
        public virtual string Action { get; }
    }

    public enum ProvisionResultStatus
    {
        Failed,

        Completed
    }

    public class SubWebCreatingEventArgs : ProvisionEventArgs
    {
        public override string Action { get; } = "SubWeb creating";
    }

    public class ProvisionCompletedEventArgs : ProvisionEventArgs
    {
        public override string Action { get; } = "Provision completed";
        public ProvisionResultStatus Status { get; }
    }

    public class ProvisionStartedEventArgs : ProvisionEventArgs
    {
        public override string Action { get; } = "Provision started";
    }
}