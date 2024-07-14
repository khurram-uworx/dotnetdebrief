using System;
using System.Collections.Generic;
using System.Linq;
using UWorx.HR.Data.Linq;
using UWorx.HR.Repositories;

namespace UWorx.HR.Data
{
    public class LinqSqlUsersRepository : IHRUsersRepository
    {
        readonly string connectionString;

        public LinqSqlUsersRepository(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public IEnumerable<HRUserInfo> GetUsers()
        {
            var list = new List<HRUserInfo>();
            using(var db = new HRDataClassesDataContext(this.connectionString))
            {
                var q = from u in db.Users
                        select new { u.FirstName, u.LastName, u.UserEmail };

                int i = 1;
                foreach(var r in q)
                {
                    list.Add(new HRUserInfo()
                    {
                        UserIndex = i,
                        UserGuid = Guid.NewGuid(),
                        UserEmail = r.UserEmail,
                        FirstName = r.FirstName,
                        MiddleName = null,
                        LastName = r.LastName
                    });

                    i++;
                }
            }

            return list;
        }

        public bool UpdateName(string email, string firstName, string middleName = null, string lastName = null)
        {
            using (var db = new HRDataClassesDataContext(this.connectionString))
            {
                var q = from u in db.Users
                        where u.UserEmail == email
                        select u; // we need to get the whole entity for update to work
                var r = q.FirstOrDefault();

                int rowAffected = 0;
                if (null != r)
                {
                    rowAffected++;
                    r.FirstName = firstName;
                    r.LastName = lastName;
                }

                if (rowAffected > 0)
                {
                    db.SubmitChanges(); // if we call it without any changes; will throw exception
                    return true;
                }

                return false;
            }
        }

        public bool UpdatePassword(string email, string password)
        {
            using (var db = new HRDataClassesDataContext(this.connectionString))
            {
                var q = from u in db.Users
                        where u.UserEmail == email
                        select u;
                var r = q.FirstOrDefault();

                int rowAffected = 0;
                if (null != r) // code of this method is very much like above method
                {
                    rowAffected++;
                    r.UserPassword = password;
                }

                if (rowAffected > 0)
                {
                    db.SubmitChanges();
                    return true;
                }

                return false;
            }
        }
    }
}
