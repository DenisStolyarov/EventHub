using EventHub.Application.Abstractions.Persistence;
using EventHub.Domain.Entities;
using EventHub.Domain.Enums;
using EventHub.Infrastructure.Persistence;
using EventHub.IntegrationTests.Abstractions;
using EventHub.IntegrationTests.Fixtures;
using EventHub.IntegrationTests.TestData;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace EventHub.IntegrationTests.Repositories;

public sealed class UserRepositoryTests(IntegrationTestFixture fixture) : RepositoryTestBase(fixture), IClassFixture<IntegrationTestFixture>
{
    [Fact]
    public async Task GetByLoginAsync_ReturnsUser_WhenExists()
    {
        // Arrange
        const string login = "testuser";

        User user = EntityProvider.CreateUser(login);

        await using AppDbContext ctx = Fixture.CreateContext();
        await using IUnitOfWork uow = Fixture.Create<IUnitOfWork>();

        ctx.Users.Add(user);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        User? result = await uow.Users.GetByLoginAsync(login, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Login.Should().Be(login);
        result.Role.Should().Be(UserRole.User);
    }

    [Fact]
    public async Task GetByLoginAsync_ReturnsNull_WhenNotFound()
    {
        // Arrange
        const string login = "nonexistent";

        await using IUnitOfWork uow = Fixture.Create<IUnitOfWork>();

        // Act
        User? result = await uow.Users.GetByLoginAsync(login, TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ExistsByLoginAsync_ReturnsTrue_WhenExists()
    {
        // Arrange
        const string login = "existsuser";

        User user = EntityProvider.CreateUser(login);

        await using AppDbContext ctx = Fixture.CreateContext();
        await using IUnitOfWork uow = Fixture.Create<IUnitOfWork>();

        ctx.Users.Add(user);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        bool result = await uow.Users.ExistsByLoginAsync(login, TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsByLoginAsync_ReturnsFalse_WhenNotFound()
    {
        // Arrange
        const string login = "ghost";

        await using IUnitOfWork uow = Fixture.Create<IUnitOfWork>();

        // Act
        bool result = await uow.Users.ExistsByLoginAsync(login, TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task Add_PersistsUser()
    {
        // Arrange
        const string login = "persistuser";

        User user = EntityProvider.CreateUser(login);

        await using IUnitOfWork uow = Fixture.Create<IUnitOfWork>();

        // Act
        uow.Users.Add(user);
        await uow.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        await using AppDbContext verifyCtx = Fixture.CreateContext();

        User? stored = await verifyCtx.Users.FindAsync([user.Id], TestContext.Current.CancellationToken);

        stored.Should().NotBeNull();
        stored.Login.Should().Be(login);
    }

    [Fact]
    public async Task Add_DuplicateLogin_ThrowsUniqueConstraintViolation()
    {
        // Arrange
        const string login = "duplogin";

        User firstUser = EntityProvider.CreateUser(login);
        User secondUser = EntityProvider.CreateUser(login);

        await using AppDbContext ctx = Fixture.CreateContext();

        ctx.Users.Add(firstUser);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        Func<Task> act = async () =>
        {
            ctx.Users.Add(secondUser);
            await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);
        };

        // Assert
        await act.Should().ThrowAsync<DbUpdateException>();
    }
}
