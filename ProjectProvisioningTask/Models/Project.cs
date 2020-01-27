using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SharePoint;

namespace ProjectProvisioningTask.Models
{
    public class Project
    {
        public string ProjectListTitle { get; }

        public SPWeb Web { get; }

        public SPListItem Item { get; }

        public Project(SPItemEventProperties properties)
        {
            ProjectListTitle = properties.ListTitle;
            Web = properties.Web;
            Item = properties.ListItem;
        }
    }
}
