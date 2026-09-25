using AwesomeAssertions;
using FlowIQ.Application.Authentication;
using FlowIQ.Application.CompaniesAndTeams.Commands.UpdateTeamMemberRole;
using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Domain.Authentication;
using FlowIQ.Domain.Exceptions;
using Moq;
using Xunit;

namespace FlowIQ.Tests.Unit.CompaniesAndTeams;

public class UpdateTeamMemberRoleCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private UpdateTeamMemberRoleCommandHandler CreateHandler() => new(_userRepository.Object, _unitOfWork.Object);

    private static User CreateUser(Guid companyId, UserRole role) =>
        new(companyId, $"{Guid.NewGuid()}@acme.com", "hash", "Jane", "Doe", role);

    [Fact]
    public async Task Handle_PromotingMemberToAdmin_Succeeds()
    {
        var companyId = Guid.NewGuid();
        var requester = CreateUser(companyId, UserRole.Owner);
        var target = CreateUser(companyId, UserRole.Member);

        _userRepository.Setup(r => r.GetByIdAsync(target.Id, It.IsAny<CancellationToken>())).ReturnsAsync(target);

        var result = await CreateHandler().Handle(
            new UpdateTeamMemberRoleCommand(companyId, requester.Id, target.Id, UserRole.Admin), CancellationToken.None);

        result.Role.Should().Be(UserRole.Admin);
        _userRepository.Verify(r => r.Update(target), Times.Once);
    }

    [Fact]
    public async Task Handle_DemotingTheOnlyOwner_ThrowsDomainException()
    {
        var companyId = Guid.NewGuid();
        var requester = CreateUser(companyId, UserRole.Owner);
        var target = CreateUser(companyId, UserRole.Owner);

        _userRepository.Setup(r => r.GetByIdAsync(target.Id, It.IsAny<CancellationToken>())).ReturnsAsync(target);
        _userRepository.Setup(r => r.CountByCompanyAndRoleAsync(companyId, UserRole.Owner, It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var act = () => CreateHandler().Handle(
            new UpdateTeamMemberRoleCommand(companyId, requester.Id, target.Id, UserRole.Admin), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<DomainException>();
        _userRepository.Verify(r => r.Update(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DemotingOneOfMultipleOwners_Succeeds()
    {
        var companyId = Guid.NewGuid();
        var requester = CreateUser(companyId, UserRole.Owner);
        var target = CreateUser(companyId, UserRole.Owner);

        _userRepository.Setup(r => r.GetByIdAsync(target.Id, It.IsAny<CancellationToken>())).ReturnsAsync(target);
        _userRepository.Setup(r => r.CountByCompanyAndRoleAsync(companyId, UserRole.Owner, It.IsAny<CancellationToken>())).ReturnsAsync(2);

        var result = await CreateHandler().Handle(
            new UpdateTeamMemberRoleCommand(companyId, requester.Id, target.Id, UserRole.Admin), CancellationToken.None);

        result.Role.Should().Be(UserRole.Admin);
    }

    [Fact]
    public async Task Handle_ChangingOwnRole_ThrowsDomainException()
    {
        var companyId = Guid.NewGuid();
        var requester = CreateUser(companyId, UserRole.Owner);

        var act = () => CreateHandler().Handle(
            new UpdateTeamMemberRoleCommand(companyId, requester.Id, requester.Id, UserRole.Admin), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<DomainException>();
        _userRepository.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
