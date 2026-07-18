using FluentAssertions;
using NetDropbox.Domain;

namespace NetDropbox.Tests.Domain;

public sealed class UserStorageAccountTests
{
    [Fact]
    public void Reserve_enforces_storage_quota()
    {
        var user = UserStorageAccount.Create("a@example.com", "A", 10);

        user.Reserve(8);
        var action = () => user.Reserve(3);

        user.UsedBytes.Should().Be(8);
        action.Should().Throw<Exception>().WithMessage("*quota*");
    }
}

