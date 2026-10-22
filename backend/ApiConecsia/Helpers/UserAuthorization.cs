namespace ApiConecsia.Helpers;

public static class UserAuthorization
{
    public static class Claims
    {
        public const string UserId = "sub";
        public const string OrganizationId = "organization_id";
        public const string Permission = "permission";
    }

    public static class Policies
    {
        public const string Authenticated = "authenticated";
    }

    public static class Roles
    {
        public const string Administrator = "Administrator";
        public const string Receptionist = "Receptionist";
        public const string Security = "Security";
    }
}
