using System.Text.Json.Serialization;
using System;
using UWorx.HR.Abstractions;

namespace HRApis.Services;

public class HRUserResponse
{
    [JsonPropertyName("firstName")]
    public string FirstName { get; set; }

    [JsonPropertyName("middleName")]
    public string MiddleName { get; set; }

    [JsonPropertyName("lastName")]
    public string LastName { get; set; }
}

public interface IUsersService
{
    IHRResult GetUserInformationByEmail(string email);
    IHRResult GetUserInformationByIndex(int userIndex);
    IHRResult GetUserInformationByGuid(Guid userGuid);
}
