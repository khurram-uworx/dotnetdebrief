using System.Numerics;
using System.Numerics.Tensors;

var tensor = Tensor.Create([
    (BFloat16)1.0f, (BFloat16)2.5f,
    (BFloat16)3.75f, (BFloat16)(-1.25f)]);
Console.WriteLine(tensor.FlattenedLength);


Func<int, UserResult> getUser = id
    => new Error("Its fun, isnt it?");

var result = getUser(42);
Console.WriteLine(result switch
{
    User u => $"Hello {u.Name}",
    NotFound n => $"Missing: {n.Reason}",
    Error e => $"Oops: {e.Message}",
});

if (result is User { Name: { Length: > 0 } } user)
{ /* we cam do something about it */ }

record User(string Name);
record NotFound(string Reason);
record Error(string Message);
union UserResult(User, NotFound, Error);
