namespace ATSolution.SharedKernel.Constants;

public static class IdentityMessages
{
    public const string ModuleRunning = "Identity module is running.";
    public const string ModuleRunningWithUsers = "Identity module is running. {0} user(s) available.";
    public const string UserEmailAlreadyExists = "A user with email '{0}' already exists.";
    public const string PartialUpdateRequiresField = "At least one field must be provided for a partial update.";
    public const string UserNotFoundByEmail = "User with email '{0}' was not found.";
    public const string UserDeletedSuccessfully = "User deleted successfully.";
}