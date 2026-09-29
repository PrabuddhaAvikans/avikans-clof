namespace ATSolution.SharedKernel.Constants;

public static class IdentityMessages
{
    public const string ModuleRunning = "Identity module is running.";
    public const string ModuleRunningWithUsers = "Identity module is running. {0} user(s) available.";
    public const string UserEmailAlreadyExists = "A user with email '{0}' already exists.";
    public const string PartialUpdateRequiresField = "At least one field must be provided for a partial update.";
    public const string UserNotFoundByEmail = "User with email '{0}' was not found.";
    public const string UserNotFoundById = "User with id '{0}' was not found.";
    public const string UserDeletedSuccessfully = "User deleted successfully.";
    public const string InvalidCredentials = "Invalid email or password.";
    public const string RoleNotFound = "Role with id '{0}' was not found.";
    public const string RoleNameAlreadyExists = "A role with name '{0}' already exists.";
    public const string RoleGroupNotFound = "Role group with id '{0}' was not found.";
    public const string RoleGroupNameAlreadyExists = "A role group with name '{0}' already exists.";
    public const string SystemRoleCannotModifyPermissions = "System roles cannot have permissions modified.";
    public const string SystemRoleCannotDelete = "System roles cannot be deleted.";
    public const string DefaultPassword = "ChangeMe@123";
    public const string AdminRoleName = "Admin";
    public const string AdminEmail = "prabuddha@avikans.com";
    public const string AdminPassword = "Avikans@123";
}