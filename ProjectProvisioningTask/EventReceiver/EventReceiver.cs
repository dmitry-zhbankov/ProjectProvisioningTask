using System;
using System.Security.Permissions;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Utilities;
using Microsoft.SharePoint.Workflow;
using System.Linq;
using System.Collections.Generic;
using static ProjectProvisioningTask.EventReceiver.ProjectStringConstants;

namespace ProjectProvisioningTask.EventReceiver
{
    public static class ProjectStringConstants
    {
        public const string ProjectListTitle = "Projects";

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

            var worker = new ProvisionWorker(properties.Web, properties.ListItem);
            worker.DoWork();
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
                ProvisionCompleted.Invoke(this, new ProvisionCompletedEventArgs());
                return;
            }

            var owners = _item[Project.Owners] as SPFieldUserValueCollection;
            var members = _item[Project.Members] as SPFieldUserValueCollection;
            var visitors = _item[Project.Visitors] as SPFieldUserValueCollection;

            if (!owners.Any(x => x.User.ID == _web.CurrentUser.ID))
            {
                ProvisionCompleted.Invoke(this, new ProvisionCompletedEventArgs());
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

            ProvisionCompleted.Invoke(this, new ProvisionCompletedEventArgs());
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

    public class SubWebCreatingEventArgs
    {        
    }

    public class ProvisionCompletedEventArgs
    {
    }

    public class ProvisionStartedEventArgs
    {
    }
}