using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SharePoint;
using Test.Project.Provisioning.Constants;

namespace Test.Project.Provisioning.Models
{
    public class Project
    {
        public string Title { get; }

        public string Description { get; }

        public  string ProjectListTitle { get; }

        public  string Address { get; }

        public string Category { get; }

        public SPFieldUserValueCollection Owners { get; }

        public SPFieldUserValueCollection Members { get; }

        public SPFieldUserValueCollection Visitors { get; }

        public  SPUser User { get; }

        //string empty
        public string Url { get; internal set; }

        public Project(SPItemEventProperties properties)
        {
            var item = properties.ListItem;

            Title = item[ProjectConstants.Project.Title] as string;
            Description = item[ProjectConstants.Project.Description] as string;
            ProjectListTitle = properties.ListTitle;
            Address = item[ProjectConstants.Project.Address] as string;
            Category = item[ProjectConstants.Project.Category] as string;

            Owners = item[ProjectConstants.Project.Owners] as SPFieldUserValueCollection;
            Members = item[ProjectConstants.Project.Members] as SPFieldUserValueCollection;
            Visitors = item[ProjectConstants.Project.Visitors] as SPFieldUserValueCollection;

            var web = properties.Web;

            User = web.CurrentUser;
            
            Url = $"{web.Url}/{Title}";
        }
    }
}
