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

                var subweb = web.Webs.Add(
                    item.Title.ToLower(),
                    item.Title, item["Description"] as string,
                    web.Language,
                    SPWebTemplate.WebTemplateSTS,
                    false,
                    false
                    );

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


            }
        }
    }
}