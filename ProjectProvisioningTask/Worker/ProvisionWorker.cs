using System;
using System.Linq;
using Microsoft.SharePoint;
using ProjectProvisioningTask.Constants;
using ProjectProvisioningTask.Models;
using ProjectProvisioningTask.ProjectEventArgs;

namespace ProjectProvisioningTask.ProjectER
{
    public class ProvisionWorker
    {
        private SPWeb _web;
        private SPListItem _item;

        public delegate void ProvisionStartedEventHandler(object sender, ProvisionStartedEventArgs e);
        public delegate void ProvisionCompletedEventHandler(object sender, ProvisionCompletedEventArgs e);
        public delegate void SubWebCreatingEventHandler(object sender, SubWebCreatingEventArgs e);

        public event ProvisionStartedEventHandler ProvisionStarted;
        public event ProvisionCompletedEventHandler ProvisionCompleted;
        public event SubWebCreatingEventHandler SubWebCreating;

        public ProvisionWorker(Project project)
        {
            _web = project.Web;
            _item = project.Item;
        }

        public void Provision()
        {
            ProvisionStarted?.Invoke(this, new ProvisionStartedEventArgs());

            if (_web.Webs.Any(x => x.Title == _item.Title))
            {
                ProvisionCompleted?.Invoke(this, new ProvisionCompletedEventArgs());
                return;
            }

            var owners = _item[ProjectConstants.Project.Owners] as SPFieldUserValueCollection;
            var members = _item[ProjectConstants.Project.Members] as SPFieldUserValueCollection;
            var visitors = _item[ProjectConstants.Project.Visitors] as SPFieldUserValueCollection;

            if (owners == null || owners.All(x => x.User.ID != _web.CurrentUser.ID))
            {
                ProvisionCompleted?.Invoke(this, new ProvisionCompletedEventArgs());
                return;
            }

            SubWebCreating?.Invoke(this, new SubWebCreatingEventArgs());

            using (var subweb = _web.Webs.Add(
                _item.Title.ToLower(),
                _item.Title, _item[ProjectConstants.Project.Description] as string,
                _web.Language,
                SPWebTemplate.WebTemplateSTS,
                false,
                false
            ))
            {
                _item[ProjectConstants.Project.Status] = ProjectConstants.ProjectStatus.InProvisioning;

                CreateSiteGroups(subweb, owners, members, visitors);

                CreateLists(subweb);

                _item[ProjectConstants.Project.Status] = ProjectConstants.ProjectStatus.Active;

                _item.Update();
            }

            ProvisionCompleted?.Invoke(this, new ProvisionCompletedEventArgs());
        }

        private void CreateLists(SPWeb subweb)
        {
            subweb.Lists.Add(ProjectConstants.Lists.Tasks, null, SPListTemplateType.Tasks);
            var guidDocs = subweb.Lists.Add(ProjectConstants.Lists.Documents, null, SPListTemplateType.DocumentLibrary);
            subweb.Lists.Add(ProjectConstants.Lists.Notes, null, SPListTemplateType.GenericList);

            var docList = subweb.Lists[guidDocs];

            AddDocLibFields(docList);

            docList.Update();
        }

        private void AddDocLibFields(SPList docList)
        {
            var titleField = docList.Fields.CreateNewField(SPFieldType.Text.ToString(), ProjectConstants.ProjectDocumentsFields.Title);

            titleField.Required = true;
            titleField.DefaultValue = _item.Title;

            var descriptionField = docList.Fields.CreateNewField(SPFieldType.Note.ToString(), ProjectConstants.ProjectDocumentsFields.Description);
            descriptionField.Required = false;
            descriptionField.DefaultValue = _item[ProjectConstants.Project.Description] as string;
            descriptionField.ShowInViewForms = true;

            var addressField = docList.Fields.CreateNewField(SPFieldType.Note.ToString(), ProjectConstants.ProjectDocumentsFields.ProjectAddress);
            addressField.Required = false;
            addressField.DefaultValue = _item[ProjectConstants.Project.Address] as string;
            addressField.ShowInViewForms = true;
            docList.Fields.Add(addressField);

            var categoryField = docList.Fields.CreateNewField(SPFieldType.Choice.ToString(), ProjectConstants.ProjectDocumentsFields.ProjectCategory);
            categoryField.Required = true;
            categoryField.DefaultValue = _item[ProjectConstants.Project.Category] as string;
            categoryField.ShowInViewForms = true;
            categoryField.ShowInEditForm = false;

            docList.Fields.Add(titleField);
            docList.Fields.Add(descriptionField);
            docList.Fields.Add(categoryField);
        }

        private void CreateSiteGroups(SPWeb subweb, SPFieldUserValueCollection owners, SPFieldUserValueCollection members, SPFieldUserValueCollection visitors)
        {
            subweb.BreakRoleInheritance(false, false);

            var strOwners = $"{_item.Title} {ProjectConstants.Project.Owners}";
            var strMembers = $"{_item.Title} {ProjectConstants.Project.Members}";
            var strVisitors = $"{_item.Title} {ProjectConstants.Project.Visitors}";

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

            var role = _web.RoleDefinitions.GetByType(SPRoleType.Guest);
        }
    }
}