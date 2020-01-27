using Microsoft.SharePoint;

namespace ProjectProvisioningTask.ProjectEventArgs
{
    public abstract class ProvisionEventArgs : System.EventArgs
    {
        public SPUser User { get; protected set; }

        public string Url { get; protected set; }

        public abstract string Action { get; }

        public string Message { get; protected set; }
    }
}