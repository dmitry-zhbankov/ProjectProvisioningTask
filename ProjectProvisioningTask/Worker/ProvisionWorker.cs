using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SharePoint;
using Test.Project.Provisioning.Models;
using Test.Project.Provisioning.Constants;
using Test.Project.Provisioning.Log;
using Test.Project.Provisioning.ProjectEventArgs;

namespace Test.Project.Provisioning.Worker
{
    public class ProvisionWorker : IPublisher
    {
        private Models.Project _project;
        private SPWeb _web;

        public delegate void ProvisionStartedEventHandler(object sender, ProvisionStartedEventArgs args);
        public delegate void ProvisionCompletedEventHandler(object sender, ProvisionCompletedEventArgs args);
        public delegate void SubWebCreatingEventHandler(object sender, SubWebCreatingEventArgs args);
        public delegate void GroupsCreatingEventHandler(object sender, GroupsCreatingEventArgs args);
        public delegate void ListsCreatingEventHandler(object sender, ListsCreatingEventArgs args);

        public event ProvisionStartedEventHandler ProvisionStarted;
        public event ProvisionCompletedEventHandler ProvisionCompleted;
        public event SubWebCreatingEventHandler SubWebCreating;
        public event GroupsCreatingEventHandler GroupsCreating;
        public event ListsCreatingEventHandler ListsCreating;

        public IList<ILogger> Loggers { get; set; }

        public ProvisionWorker(Models.Project project, SPWeb web)
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

            ProvisionCompleted += (sender, args) =>
            {
                foreach (var logger in Loggers)
                {
                    logger.Log(args.Action,
                        args.Status == ProvisionResultStatus.Succeed ? LogSeverity.Information : LogSeverity.Error);
                }
            };

            SubWebCreating += (sender, args) =>
            {
                foreach (var logger in Loggers)
                {
                    logger.Log(args.Action, LogSeverity.Information);
                }
            };

            GroupsCreating += (sender, args) =>
            {
                foreach (var logger in Loggers)
                {
                    logger.Log(args.Action, LogSeverity.Information);
                }
            };
        }

        public void Provision()
        {
            ProvisionStarted?.Invoke(this, new ProvisionStartedEventArgs(_project.Url, _project.User));

            if (_web.Webs.Any(x => x.Title == _project.Title))
            {
                ProvisionCompleted?.Invoke(this, new ProvisionCompletedEventArgs(_project.Url, _project.User, ProvisionResultStatus.Failed));
                return;
            }

            if (_project.Owners == null || _project.Owners.All(x => x.User.ID != _project.User.ID))
            {
                ProvisionCompleted?.Invoke(this, new ProvisionCompletedEventArgs(_project.Url, _project.User, ProvisionResultStatus.Failed));
                return;
            }

            CreateSubWeb();

            ProvisionCompleted?.Invoke(this, new ProvisionCompletedEventArgs(_project.Url, _project.User, ProvisionResultStatus.Succeed));
        }

        private void CreateSubWeb()
        {
            SubWebCreating?.Invoke(this, new SubWebCreatingEventArgs(_project.Url, _project.User));

            try
            {
                using (var subweb = _web.Webs.Add(
                    _project.Title,
                    _project.Title, _project.Description,
                    _web.Language,
                    SPWebTemplate.WebTemplateSTS,
                    false,
                    false
                ))
                {
                    CreateSiteGroups(subweb, _project.Owners, _project.Members, _project.Visitors);

                    CreateLists(subweb);
                }
            }
            catch
            {
                ProvisionCompleted?.Invoke(this, new ProvisionCompletedEventArgs(_project.Url, _project.User, ProvisionResultStatus.Failed,ProjectConstants.ProjectExceptions.WorkerExceptions.SubWebCreationFailed));
            }
        }

        private void CreateLists(SPWeb web)
        {
            ListsCreating?.Invoke(this, new ListsCreatingEventArgs(_project.Url, _project.User));

            try
            {
                web.Lists.Add(ProjectConstants.Lists.Tasks, null, SPListTemplateType.Tasks);
                var guidDocs = web.Lists.Add(ProjectConstants.Lists.Documents, null, SPListTemplateType.DocumentLibrary);
                web.Lists.Add(ProjectConstants.Lists.Notes, null, SPListTemplateType.GenericList);

                var docList = web.Lists[guidDocs];

                AddDocLibFields(docList);
            }
            catch
            {
                ProvisionCompleted?.Invoke(this, new ProvisionCompletedEventArgs(_project.Url, _project.User, ProvisionResultStatus.Failed, ProjectConstants.ProjectExceptions.WorkerExceptions.SiteListsCreationFailed));
            }
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
            view.ViewFields.Add(docList.Fields.GetFieldByInternalName(strTitleField));
            view.ViewFields.Add(docList.Fields.GetFieldByInternalName(strDescriptionField));
            view.ViewFields.Add(docList.Fields.GetFieldByInternalName(strAddressField));
            view.ViewFields.Add(docList.Fields.GetFieldByInternalName(strCategoryField));

            view.Update();
        }

        private void CreateSiteGroups(SPWeb web, SPFieldUserValueCollection owners, SPFieldUserValueCollection members, SPFieldUserValueCollection visitors)
        {
            GroupsCreating?.Invoke(this, new GroupsCreatingEventArgs(_project.Url, _project.User));

            try
            {
                web.BreakRoleInheritance(false, false);

                AddSiteGroup(web, owners, ProjectConstants.Project.Owners, _web.RoleDefinitions.GetByType(SPRoleType.Administrator));
                AddSiteGroup(web, members, ProjectConstants.Project.Members, _web.RoleDefinitions.GetByType(SPRoleType.Editor));
                AddSiteGroup(web, visitors, ProjectConstants.Project.Visitors, _web.RoleDefinitions.GetByType(SPRoleType.Reader));
            }
            catch
            {
                ProvisionCompleted?.Invoke(this, new ProvisionCompletedEventArgs(_project.Url, _project.User, ProvisionResultStatus.Failed, ProjectConstants.ProjectExceptions.WorkerExceptions.SiteGroupsCreationFailed));
            }
        }

        private void AddSiteGroup(SPWeb web, SPFieldUserValueCollection users, string group, SPRoleDefinition role)
        {
            if (users==null)
            {
                return;
            }

            var strGroup = $"{_project.Title} {group}";
            web.SiteGroups.Add(strGroup, web.CurrentUser, null, null);

            web.SiteGroups[strGroup].Users.AddCollection(users.Select(x => new SPUserInfo()
            {
                Name = x.User.Name,
                Email = x.User.Email,
                LoginName = x.User.LoginName,
                Notes = x.User.Notes
            })
                .ToArray());

            foreach (var user in users)
            {
                var roleAssignment = new SPRoleAssignment(user.User);
                roleAssignment.RoleDefinitionBindings.Add(role);
                web.RoleAssignments.Add(roleAssignment);
            }
        }
    }
}
