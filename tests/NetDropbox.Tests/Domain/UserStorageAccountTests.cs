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

    [Fact]
    public void Reserve_rejects_overflow_without_changing_usage()
    {
        var user = UserStorageAccount.Create("a@example.com", "A", long.MaxValue);
        user.Reserve(long.MaxValue - 1);

        var action = () => user.Reserve(10);

        action.Should().Throw<Exception>().WithMessage("*quota*");
        user.UsedBytes.Should().Be(long.MaxValue - 1);
    }

    [Fact]
    public void Quota_preflight_does_not_mutate_usage()
    {
        var user = UserStorageAccount.Create("a@example.com", "A", 10);

        user.EnsureCanReserve(8);

        user.UsedBytes.Should().Be(0);
    }
}
