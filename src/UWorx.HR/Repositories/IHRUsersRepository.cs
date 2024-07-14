using System;
using System.Collections.Generic;

namespace UWorx.HR.Repositories
{
    public class HRUserInfo
    {
        public int UserIndex { get; set; }
        public Guid UserGuid { get; set; }
        public string UserEmail { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
    }

    public interface IHRUsersRepository
    {
        IEnumerable<HRUserInfo> GetUsers();
        bool UpdatePassword(string email, string password);
        bool UpdateName(string email, string firstName, string middleName = null, string lastName = null);
    }
}
