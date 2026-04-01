namespace DriverGuardian.Application.Logging.Enums;

public enum RecoverabilityHint
{
    Unknown = 0,
    Retryable = 1,
    UserActionRequired = 2,
    ManualVerificationRequired = 3,
    NotRecoverable = 4
}
