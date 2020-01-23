using System;
using System.Security.Permissions;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Utilities;
using Microsoft.SharePoint.Workflow;
using System.Linq;

namespace ProjectProvisioningTask.EventReceiver
{
    /// <summary>
    /// List Item Events
    /// </summary>
    public class EventReceiver : SPItemEventReceiver
    {
        /// <summary>
        /// An item was added
        /// </summary>
        public override void ItemAdded(SPItemEventProperties properties)
        {
            if (properties.ListTitle != "Projects")
            {
                return;
            }

            using (var web = properties.Web)
            {
                var item = properties.ListItem;

                var owners = item["Owners"] as SPFieldUserValueCollection;
                var members = item["Members"] as SPFieldUserValueCollection;
                var visitors = item["Visitors"] as SPFieldUserValueCollection;

                if (!owners.Any(x => x.User.ID == web.CurrentUser.ID))
                {
                    return;
                }

                using (var subweb = web.Webs.Add(
                    item.Title.ToLower(),
                    item.Title, item["Description"] as string,
                    web.Language,
                    SPWebTemplate.WebTemplateSTS,
                    false,
                    false
                    ))
                {
                    item["Project Status"] = "In Provisioning";

                    subweb.BreakRoleInheritance(false, false);

                    var strOwners = $"{item.Title} Owners";
                    var strMembers = $"{item.Title} Members";
                    var strVisitors = $"{item.Title} Visitors";

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

                    subweb.Lists.Add("Project Tasks", null, SPListTemplateType.Tasks);
                    var guidDocs = subweb.Lists.Add("Project Documents", null, SPListTemplateType.DocumentLibrary);
                    subweb.Lists.Add("Project Notes", null, SPListTemplateType.GenericList);

                    var docList = subweb.Lists[guidDocs];

                    var titleField = docList.Fields.CreateNewField(SPFieldType.Text.ToString(), "Project Title");
                    titleField.Required = true;
                    titleField.DefaultValue = item.Title;
                    docList.Fields.Add(titleField);

                    var descriptionField = docList.Fields.CreateNewField(SPFieldType.Note.ToString(), "Project Description");
                    descriptionField.Required = false;
                    descriptionField.DefaultValue = item["Description"] as string;
                    docList.Fields.Add(descriptionField);

                    var addressField = docList.Fields.CreateNewField(SPFieldType.Note.ToString(), "Project Address");
                    addressField.Required = false;
                    addressField.DefaultValue = item["Address"] as string;
                    docList.Fields.Add(addressField);

                    var categoryField = docList.Fields.CreateNewField(SPFieldType.Choice.ToString(), "Project Category");
                    categoryField.Required = true;
                    categoryField.DefaultValue = item["Category"] as string;
                    docList.Fields.Add(categoryField);

                    docList.Update();

                    item["Project Status"] = "Active";

                    item.Update();
                }
            }
        }
    }
}