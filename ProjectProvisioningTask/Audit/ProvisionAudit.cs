using Microsoft.SharePoint;
using ProjectProvisioningTask.Constants;
using ProjectProvisioningTask.Models;

namespace ProjectProvisioningTask.Audit
{
    public class ProvisionAudit
    {
        private SPWeb _web;
        private SPList _list;

        public ProvisionAudit(Project project)
        {
            _web = project.Web;

            _list = _web.Lists[ProjectConstants.AuditListTitle];

            if (_list == null)
            {
                CreateAuditList();
            }
        }

        public void Audit(string user, string url, string action)
        {
            var item = _list.AddItem();
            item[ProjectConstants.ProjectAuditFields.User] = user;
            item[ProjectConstants.ProjectAuditFields.Url] = url;
            item[ProjectConstants.ProjectAuditFields.Action] = action;
            item.Update();
        }

        private void CreateAuditList()
        {
            var guidAuditList = _web.Lists.Add(ProjectConstants.AuditListTitle, null, SPListTemplateType.GenericList);
            _list = _web.Lists[guidAuditList];

            var userField = _list.Fields.CreateNewField(SPFieldType.Text.ToString(), ProjectConstants.ProjectAuditFields.User);
            userField.Required = false;
            userField.ShowInViewForms = true;
            _list.Fields.Add(userField);

            var urlField = _list.Fields.CreateNewField(SPFieldType.Text.ToString(), ProjectConstants.ProjectAuditFields.Url);
            userField.Required = false;
            userField.ShowInViewForms = true;
            _list.Fields.Add(urlField);

            var actionField = _list.Fields.CreateNewField(SPFieldType.Text.ToString(), ProjectConstants.ProjectAuditFields.Url);
            userField.Required = false;
            userField.ShowInViewForms = true;
            _list.Fields.Add(actionField);

            _list.Fields.Add(userField);
            _list.Fields.Add(urlField);
            _list.Fields.Add(actionField);
        }
    }
}