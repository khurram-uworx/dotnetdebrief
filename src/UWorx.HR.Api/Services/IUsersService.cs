using System.Text.Json.Serialization;
using UWorx.HR.Abstractions;

namespace UWorx.HR.Api.Services
{
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
}
