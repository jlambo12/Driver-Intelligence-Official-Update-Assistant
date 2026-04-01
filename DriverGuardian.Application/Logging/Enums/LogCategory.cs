namespace DriverGuardian.Application.Logging.Enums;

public enum LogCategory
{
    ApplicationLifecycle = 0,
    UserAction = 1,
    ScanPipeline = 2,
    DeviceDiscovery = 3,
    DriverInspection = 4,
    ProviderMatching = 5,
    VersionComparison = 6,
    DownloadFlow = 7,
    InstallFlow = 8,
    VerificationFlow = 9,
    Audit = 10,
    Diagnostics = 11,
    UnexpectedException = 12
}
