using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using InsuranceClaimSystem.Application.Common;
using InsuranceClaimSystem.Application.DTOs.Nominees;
using InsuranceClaimSystem.Application.Interfaces.Repositories;
using InsuranceClaimSystem.Domain.Entities;
using InsuranceClaimSystem.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace InsuranceClaimSystem.Tests.UnitTests.Services
{
    public class NomineeServiceTests
    {
        private readonly Mock<INomineeRepository> _nomineeRepositoryMock;
        private readonly Mock<IPolicyRepository> _policyRepositoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<NomineeService>> _loggerMock;
        private readonly NomineeService _nomineeService;

        public NomineeServiceTests()
        {
            _nomineeRepositoryMock = new Mock<INomineeRepository>();
            _policyRepositoryMock = new Mock<IPolicyRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<NomineeService>>();

            _nomineeService = new NomineeService(
                _nomineeRepositoryMock.Object,
                _policyRepositoryMock.Object,
                _unitOfWorkMock.Object,
                _mapperMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task AddNomineeAsync_WhenPolicyNotFound_ReturnsFailure()
        {
            // Arrange
            var policyId = Guid.NewGuid();
            var request = new NomineeRequest { FullName = "Jane Doe" };

            _policyRepositoryMock.Setup(repo => repo.GetByIdAsync(policyId))
                .ReturnsAsync((Policy?)null);

            // Act
            var result = await _nomineeService.AddNomineeAsync(policyId, Guid.NewGuid(), request);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("PolicyNotFound");
            _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(default), Times.Never);
        }

        [Fact]
        public async Task AddNomineeAsync_WhenExistingActiveNominee_DisablesExistingAndAddsNew()
        {
            // Arrange
            var policyId = Guid.NewGuid();
            var policy = new Policy { Id = policyId, PolicyHolderId = Guid.NewGuid() };
            var existingNominee = new Nominee { Id = Guid.NewGuid(), IsActive = true };
            var request = new NomineeRequest { FullName = "New Nominee" };
            var mappedDto = new NomineeDto { Id = Guid.NewGuid(), FullName = "New Nominee" };

            _policyRepositoryMock.Setup(repo => repo.GetByIdAsync(policyId))
                .ReturnsAsync(policy);

            _nomineeRepositoryMock.Setup(repo => repo.GetActiveNomineeByPolicyIdAsync(policyId))
                .ReturnsAsync(existingNominee);

            _mapperMock.Setup(m => m.Map<NomineeDto>(It.IsAny<Nominee>()))
                .Returns(mappedDto);

            // Act
            var result = await _nomineeService.AddNomineeAsync(policyId, Guid.NewGuid(), request);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(mappedDto);
            existingNominee.IsActive.Should().BeFalse();
            _nomineeRepositoryMock.Verify(repo => repo.UpdateAsync(existingNominee), Times.Once);
            _nomineeRepositoryMock.Verify(repo => repo.AddAsync(It.Is<Nominee>(n => n.FullName == "New Nominee" && n.IsActive)), Times.Once);
            _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task GetNomineesByPolicyAsync_ReturnsMappedNominees()
        {
            // Arrange
            var policyId = Guid.NewGuid();
            var nominees = new List<Nominee> { new Nominee { Id = Guid.NewGuid() } };
            var nomineeDtos = new List<NomineeDto> { new NomineeDto { Id = nominees[0].Id } };

            _nomineeRepositoryMock.Setup(repo => repo.GetByPolicyIdAsync(policyId))
                .ReturnsAsync(nominees);

            _mapperMock.Setup(m => m.Map<List<NomineeDto>>(nominees))
                .Returns(nomineeDtos);

            // Act
            var result = await _nomineeService.GetNomineesByPolicyAsync(policyId);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().BeEquivalentTo(nomineeDtos);
        }
    }
}
