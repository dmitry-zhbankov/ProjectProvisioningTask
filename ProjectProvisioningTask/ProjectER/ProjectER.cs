using System.Security.Permissions;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Utilities;
using Microsoft.SharePoint.Workflow;
using System.Collections.Generic;
using ProjectProvisioningTask.Audit;
using ProjectProvisioningTask.Logger;
using ProjectProvisioningTask.Models;
using static ProjectProvisioningTask.Constants.ProjectConstants;
using Project = ProjectProvisioningTask.Models.Project;

namespace ProjectProvisioningTask.ProjectER
{
    /// <summary>
    /// List Item Events
    /// </summary>
    public class ProjectER : SPItemEventReceiver
    {
        /// <summary>
        /// An _item was added
        /// </summary>
        public override void ItemAdded(SPItemEventProperties properties)
        {
            base.ItemAdded(properties);

            var project=new Project(properties);

            if (project.ProjectListTitle != ProjectListTitle)
            {
                return;
            }

            ILogger logger = new ULSLogger();

            var audit = new ProvisionAudit(project);

            var worker = new ProvisionWorker(project);

            worker.ProvisionStarted += (sender, args) =>
            {
                logger.LogInformation(args.Action);
            };

            worker.Provision();
        }
    }
}