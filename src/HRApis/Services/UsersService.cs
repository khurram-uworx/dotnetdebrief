using System.Collections.Generic;
using System;
using UWorx.HR.Abstractions;
using UWorx.HR.Implementations;
using UWorx.HR.Repositories;
using System.Linq;

namespace HRApis.Services;

public class UserService : IUsersService
{
    Lazy<List<HRUserInfo>> lazyData;
    readonly IHRUsersRepository repository;

    public UserService(IHRUsersRepository repository)
    {
        this.repository = repository;
        this.lazyData = new Lazy<List<HRUserInfo>>(
            () => repository.GetUsers().ToList());
    }

    IHRResult getResult(HRUserInfo? r, string failedMessage)
    {
        if (null != r)
            return new HRDataResult<HRUserResponse>(new()
            {
                FirstName = r.FirstName,
                MiddleName = r.MiddleName,
                LastName = r.LastName
            });
        else
            return new HRResult().AddError(failedMessage);
    }

    public IHRResult GetUserInformationByEmail(string email)
    {
        IEnumerable<HRUserInfo> data;

        try
        {
            data = this.lazyData.Value;
        }
        catch (Exception ex)
        {
            return new HRResult().AddError(ex.Message);
        }

        var q = from u in data
                where u.UserEmail == email
                select u;

        return this.getResult(q.FirstOrDefault(),
            $"Failed to find user with email {email}");
    }

    public IHRResult GetUserInformationByGuid(Guid userGuid)
    {
        var q = from u in this.lazyData.Value
                where u.UserGuid == userGuid
                select u;

        return this.getResult(q.FirstOrDefault(),
            $"Failed to find user with guid {userGuid}");
    }

    public IHRResult GetUserInformationByIndex(int userIndex)
    {
        var q = from u in this.lazyData.Value
                where u.UserIndex == userIndex
                select u;

        return this.getResult(q.FirstOrDefault(),
            $"Failed to find user with index {userIndex}");
    }
}
