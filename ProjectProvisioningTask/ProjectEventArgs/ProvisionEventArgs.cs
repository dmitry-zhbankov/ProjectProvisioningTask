using Microsoft.SharePoint;

namespace Test.Project.Provisioning.ProjectEventArgs
{
    public abstract class ProvisionEventArgs : System.EventArgs
    {
        public SPUser User { get; protected set; }

        public string Url { get; protected set; }

        public abstract string Action { get; }

        protected ProvisionEventArgs(string url, SPUser user)
        {
            Url = url;
            User = user;
        }
    }
}
