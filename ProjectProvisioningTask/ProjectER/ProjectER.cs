using System;
using System.Security.Permissions;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Utilities;
using Microsoft.SharePoint.Workflow;
using System.Collections.Generic;
using Test.Project.Provisioning.Models;
using Test.Project.Provisioning.Constants;
using Test.Project.Provisioning.Log;
using Test.Project.Provisioning.Worker;
using Project = Test.Project.Provisioning.Models.Project;

namespace Test.Project.Provisioning.ProjectER
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
            if (properties.ListTitle != ProjectConstants.ProjectListTitle)
            {
                return;
            }

            IExtendedLogger logger = new ULSLogger();

            try
            {
                base.ItemAdded(properties);

                var item = properties.ListItem;
                var web = properties.Web;

                var completion = new CompletionComponent(item);

                var project = new Models.Project(properties);

                ILogger audit = new ProvisionAudit(project, web);

                var worker = new ProvisionWorker(project, web);
                
                worker.Loggers.Add(logger);
                worker.Loggers.Add(audit);

                worker.ProvisionStarted += (sender, args) =>
                {
                    completion.SetStatus(ProjectConstants.ProjectStatus.InProvisioning);
                };

                worker.ProvisionCompleted += (sender, args) =>
                {
                    if (args.Status==ProvisionResultStatus.Succeed)
                    {
                        completion.SetStatus(ProjectConstants.ProjectStatus.Active);
                    }
                };
                
                worker.Provision();
            }
            catch (Exception e)
            {
                logger.LogError(e.Message);
            }
        }
    }
}
