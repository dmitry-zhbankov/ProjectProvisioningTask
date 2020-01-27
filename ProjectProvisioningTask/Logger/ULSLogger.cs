using Microsoft.SharePoint.Administration;

namespace ProjectProvisioningTask.Logger
{
    public class ULSLogger : ILogger
    {
        private SPDiagnosticsService _service;

        public ULSLogger()
        {
            _service = SPDiagnosticsService.Local;
        }

        public void LogError(string message)
        {
            Log(TraceSeverity.Unexpected, EventSeverity.Error, message);
        }

        public void LogInformation(string message)
        {
            Log(TraceSeverity.Verbose, EventSeverity.Information, message);
        }

        public void LogWarning(string message)
        {
            Log(TraceSeverity.Medium, EventSeverity.Warning, message);
        }

        private void Log(TraceSeverity traceSeverity, EventSeverity eventSeverity, string message)
        {
            _service.WriteTrace(0, new SPDiagnosticsCategory("My Category", traceSeverity, eventSeverity), traceSeverity, message);
        }
    }
}