namespace UWorx.HR.Repositories
{
    public interface IHRUsersRepository
    {
        HRUserInfo GetUserInformation(string email);
        bool UpdatePassword(string email, string password);
        bool UpdateName(string email, string firstName, string middleName = null, string lastName = null);
    }
}
