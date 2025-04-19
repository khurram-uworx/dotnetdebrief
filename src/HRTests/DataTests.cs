using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using UWorx.HR.Data;
using UWorx.HR.Repositories;

namespace HRTests;

public class DataTests
{
    const string connectionString = "Server=.;Database=HumanResources;Integrated Security=True;";
    //TrustServerCertificate=True;Application Name=
    string testUser = "test@email.com";

    public DataTests()
    {
        using (var connection = new SqlConnection(connectionString))
        {
            try
            {
                connection.Open();
                Debug.WriteLine("Connection successful!");

                string sql = "select UserIndex from users where UserEmail = @email";
                var commandSelect = new SqlCommand(sql, connection);
                commandSelect.Parameters.Add(new SqlParameter("@email", this.testUser));
                int userIndex = Convert.ToInt32(commandSelect.ExecuteScalar()); // if row is missing we will get null
                if (userIndex <= 0)                                             // convert will make it 0
                {
                    //we need to create a test user
                    sql = "insert into users (UserEmail, UserPassword, FirstName) values (@email, 'SomeRandomValue', 'First')";
                    var commandInsert = new SqlCommand(sql, connection);
                    commandInsert.Parameters.Add(new SqlParameter("@email", this.testUser));
                    int affectedRows = commandInsert.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error: " + ex.Message);
            }
        }
    }

    void usersRepositoryTests(IHRUsersRepository repository)
    {
        var list = new List<HRUserInfo>();
        list.AddRange(repository.GetUsers());

        Assert.IsTrue(list.Count > 0);
        Assert.IsTrue(list[0].FirstName.Length > 0);

        var queriedUser = list.Find(u => u.UserEmail == this.testUser);
        Assert.IsTrue(null != queriedUser);
        Assert.IsTrue(queriedUser.UserEmail == this.testUser);

        bool pass1 = repository.UpdatePassword(this.testUser, "NewRandomPassword1");
        bool pass2 = repository.UpdatePassword(this.testUser, "NewRandomPassword");
        Assert.IsTrue(pass1 || pass2); // one of this attempt should pass

        bool name1 = repository.UpdateName(this.testUser, "First Name 1", middleName: null, lastName: null);
        bool name2 = repository.UpdateName(this.testUser, "First Name", middleName: null, lastName: null);
        Assert.IsTrue(name1 || name2);
    }

    [Test]
    public void SqlTestScenario()
    {
        IHRUsersRepository repository = new SqlUsersRepository(connectionString);
        usersRepositoryTests(repository);
    }
}
