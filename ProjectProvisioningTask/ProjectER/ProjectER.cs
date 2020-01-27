using System;
using System.Security.Permissions;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Utilities;
using Microsoft.SharePoint.Workflow;
using System.Collections.Generic;
using ProjectProvisioningTask.Audit;
using ProjectProvisioningTask.Constants;
using ProjectProvisioningTask.Logger;
using ProjectProvisioningTask.Models;
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
            if (properties.ListTitle != ProjectConstants.ProjectListTitle)
            {
                return;
            }

            ILogger logger = new ULSLogger();

            try
            {
                base.ItemAdded(properties);

                var item = properties.ListItem;

                var web = properties.Web;

                var completion = new CompletionComponent(item);

                var project = new Project(properties);

                ILogger audit = new ProvisionAudit(project, web);

                var worker = new ProvisionWorker(project, web);

                worker.Loggers.Add(logger);
                worker.Loggers.Add(audit);

                //worker.ProvisionStarted += (sender, args) =>
                //{
                //    logger.Log(args.Action, LogSeverity.Information);
                //};

                completion.SetStatus(ProjectConstants.ProjectStatus.InProvisioning);

                worker.ProvisionCompleted += (sender, args) =>
                {
                    if (args.Status==ProvisionResultStatus.Completed)
                    {
                        completion.SetStatus(ProjectConstants.ProjectStatus.Active);
                    }
                };
                
                worker.Provision();
            }
            catch (Exception ex)
            {
                logger.Log(ex.Message,LogSeverity.Error);
            }
        }
    }

    public class CompletionComponent
    {
        private SPItem _item;

        public CompletionComponent(SPItem item)
        {
            _item = item;
        }

        public void SetStatus(string status)
        {
            _item[ProjectConstants.Project.Status] = status;
            _item.Update();
        }
    }
}
