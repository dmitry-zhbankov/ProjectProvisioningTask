using Microsoft.SharePoint;
using Test.Project.Provisioning.Constants;

namespace Test.Project.Provisioning.Worker
{
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
