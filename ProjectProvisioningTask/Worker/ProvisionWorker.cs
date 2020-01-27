using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SharePoint;
using ProjectProvisioningTask.Constants;
using ProjectProvisioningTask.Log;
using ProjectProvisioningTask.Logger;
using ProjectProvisioningTask.Models;
using ProjectProvisioningTask.ProjectEventArgs;

namespace ProjectProvisioningTask.ProjectER
{
    public class ProvisionWorker : IPublisher
    {
        private Project _project;
        private SPWeb _web;

        public delegate void ProvisionStartedEventHandler(object sender, ProvisionStartedEventArgs args);
        public delegate void ProvisionCompletedEventHandler(object sender, ProvisionCompletedEventArgs args);
        public delegate void SubWebCreatingEventHandler(object sender, SubWebCreatingEventArgs args);

        public event ProvisionStartedEventHandler ProvisionStarted;
        public event ProvisionCompletedEventHandler ProvisionCompleted;
        public event SubWebCreatingEventHandler SubWebCreating;

        public IList<ILogger> Loggers { get; set; }

        public ProvisionWorker(Project project, SPWeb web)
        {
            _project = project;
            _web = web;

            Loggers = new List<ILogger>();

            ProvisionStarted += (sender, args) =>
            {
                foreach (var logger in Loggers)
                {
                    logger.Log(args.Action, LogSeverity.Information);
                }
            };
        }

        public void Provision()
        {
            ProvisionStarted?.Invoke(this, new ProvisionStartedEventArgs());

            if (_web.Webs.Any(x => x.Title == _project.Title))
            {
                ProvisionCompleted?.Invoke(this, new ProvisionCompletedEventArgs());
                return;
            }

            if (_project.Owners == null || _project.Owners.All(x => x.User.ID != _project.User.ID))
            {
                ProvisionCompleted?.Invoke(this, new ProvisionCompletedEventArgs());
                return;
            }

            SubWebCreating?.Invoke(this, new SubWebCreatingEventArgs());

            using (var subweb = _web.Webs.Add(
                _project.Title.ToLower(),
                _project.Title, _project.Description,
                _web.Language,
                SPWebTemplate.WebTemplateSTS,
                false,
                false
            ))
            {
                _project.Url = subweb.Url;

                CreateSiteGroups(subweb, _project.Owners, _project.Members, _project.Visitors);

                CreateLists(subweb);
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
        }

        private void AddDocLibFields(SPList docList)
        {
            var titleField = docList.Fields.CreateNewField(SPFieldType.Text.ToString(), ProjectConstants.ProjectDocumentsFields.Title);

            titleField.Required = true;
            titleField.DefaultValue = _project.Title;

            var descriptionField = docList.Fields.CreateNewField(SPFieldType.Note.ToString(), ProjectConstants.ProjectDocumentsFields.Description);
            descriptionField.Required = false;
            descriptionField.DefaultValue = _project.Description;
            descriptionField.ShowInViewForms = true;
            descriptionField.ShowInDisplayForm = true;

            var addressField = docList.Fields.CreateNewField(SPFieldType.Note.ToString(), ProjectConstants.ProjectDocumentsFields.ProjectAddress);
            addressField.Required = false;
            addressField.DefaultValue = _project.Address;
            addressField.ShowInViewForms = true;
            addressField.ShowInDisplayForm = true;

            var categoryField = docList.Fields.CreateNewField(SPFieldType.Choice.ToString(), ProjectConstants.ProjectDocumentsFields.ProjectCategory);
            categoryField.Required = true;
            categoryField.DefaultValue = _project.Category;
            categoryField.ShowInViewForms = true;
            categoryField.ShowInEditForm = false;
            categoryField.ShowInDisplayForm = true;

            var strTitleField = docList.Fields.Add(titleField);
            var strDescriptionField = docList.Fields.Add(descriptionField);
            var strAddressField = docList.Fields.Add(addressField);
            var strCategoryField = docList.Fields.Add(categoryField);

            docList.Update();

            var view = docList.DefaultView;
            view.ViewFields.Add(docList.Fields.GetFieldByInternalName(strDescriptionField));
            view.ViewFields.Add(docList.Fields.GetFieldByInternalName(strAddressField));
            view.ViewFields.Add(docList.Fields.GetFieldByInternalName(strCategoryField));

            view.Update();
        }

        private void CreateSiteGroups(SPWeb subweb, SPFieldUserValueCollection owners, SPFieldUserValueCollection members, SPFieldUserValueCollection visitors)
        {
            subweb.BreakRoleInheritance(false, false);

            AddSiteGroup(subweb, owners, ProjectConstants.Project.Owners);
            AddSiteGroup(subweb, members, ProjectConstants.Project.Members);
            AddSiteGroup(subweb, visitors, ProjectConstants.Project.Visitors);

            //var role = _web.RoleDefinitions.GetByType(SPRoleType.Guest);
        }

        private void AddSiteGroup(SPWeb web, SPFieldUserValueCollection users, string group)
        {
            var strGroup = $"{_project.Title} {group}";
            web.SiteGroups.Add(strGroup, web.CurrentUser, null, null);

            Func<SPFieldUserValue, SPUserInfo> del = x => new SPUserInfo()
            {
                Name = x.User.Name,
                Email = x.User.Email,
                LoginName = x.User.LoginName,
                Notes = x.User.Notes
            };

            web.SiteGroups[strGroup].Users.AddCollection(users?.Select(del)?.ToArray());
        }
    }
}
