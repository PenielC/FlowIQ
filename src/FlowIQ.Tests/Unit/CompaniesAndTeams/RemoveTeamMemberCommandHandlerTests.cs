using AwesomeAssertions;
using FlowIQ.Application.Authentication;
using FlowIQ.Application.CompaniesAndTeams.Commands.RemoveTeamMember;
using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Domain.Authentication;
using FlowIQ.Domain.Exceptions;
using Moq;
using Xunit;

namespace FlowIQ.Tests.Unit.CompaniesAndTeams;

public class RemoveTeamMemberCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private RemoveTeamMemberCommandHandler CreateHandler() => new(_userRepository.Object, _unitOfWork.Object);

    private static User CreateUser(Guid companyId, UserRole role) =>
        new(companyId, $"{Guid.NewGuid()}@acme.com", "hash", "Jane", "Doe", role);

    [Fact]
    public async Task Handle_RemovingAMember_Succeeds()
    {
        var companyId = Guid.NewGuid();
        var requester = CreateUser(companyId, UserRole.Owner);
        var target = CreateUser(companyId, UserRole.Member);

        _userRepository.Setup(r => r.GetByIdAsync(target.Id, It.IsAny<CancellationToken>())).ReturnsAsync(target);

        await CreateHandler().Handle(new RemoveTeamMemberCommand(companyId, requester.Id, target.Id), CancellationToken.None);

        _userRepository.Verify(r => r.Remove(target), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_RemovingTheOnlyOwner_ThrowsDomainException()
    {
        var companyId = Guid.NewGuid();
        var requester = CreateUser(companyId, UserRole.Owner);
        var target = CreateUser(companyId, UserRole.Owner);

        _userRepository.Setup(r => r.GetByIdAsync(target.Id, It.IsAny<CancellationToken>())).ReturnsAsync(target);
        _userRepository.Setup(r => r.CountByCompanyAndRoleAsync(companyId, UserRole.Owner, It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var act = () => CreateHandler().Handle(new RemoveTeamMemberCommand(companyId, requester.Id, target.Id), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<DomainException>();
        _userRepository.Verify(r => r.Remove(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RemovingSelf_ThrowsDomainException()
    {
        var companyId = Guid.NewGuid();
        var requester = CreateUser(companyId, UserRole.Owner);

        var act = () => CreateHandler().Handle(new RemoveTeamMemberCommand(companyId, requester.Id, requester.Id), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<DomainException>();
        _userRepository.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_TargetFromAnotherCompany_ThrowsDomainException()
    {
        var requester = CreateUser(Guid.NewGuid(), UserRole.Owner);
        var target = CreateUser(Guid.NewGuid(), UserRole.Member);

        _userRepository.Setup(r => r.GetByIdAsync(target.Id, It.IsAny<CancellationToken>())).ReturnsAsync(target);

        var act = () => CreateHandler().Handle(
            new RemoveTeamMemberCommand(Guid.NewGuid(), requester.Id, target.Id), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<DomainException>();
    }
}
