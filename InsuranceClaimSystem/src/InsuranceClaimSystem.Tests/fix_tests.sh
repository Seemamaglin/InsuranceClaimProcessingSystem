sed -i '' 's/_mapper, claimLogger.Object/_mapper, new Mock<INotificationService>().Object, claimLogger.Object/g' IntegrationTests/ClaimLifecycleIntegrationTest.cs
sed -i '' 's/_mapper, policyLogger.Object/_mapper, policyLogger.Object, new Mock<INotificationService>().Object, nomineeRepository/g' IntegrationTests/ClaimLifecycleIntegrationTest.cs
sed -i '' 's/ApplyForPolicyRequest/ApplyForPolicyDto/g' IntegrationTests/ClaimLifecycleIntegrationTest.cs
sed -i '' '/EndDate = /d' IntegrationTests/ClaimLifecycleIntegrationTest.cs

sed -i '' 's/_notificationServiceMock.Object,/& _unitOfWorkMock.Object,/g' UnitTests/Services/ClaimServiceTests.cs
sed -i '' 's/ClaimAmount/CalculatedAmount/g' UnitTests/Services/ClaimServiceTests.cs
sed -i '' 's/ApproveClaimAsync/UpdateStatusAsync/g' UnitTests/Services/ClaimServiceTests.cs
sed -i '' 's/UpdateStatusAsync(claimId, reviewerId, "Approved")/UpdateStatusAsync(claimId, new UpdateClaimStatusRequest { Status = ClaimStatus.Approved, ReviewerNotes = "Approved", ActionUserId = reviewerId })/g' UnitTests/Services/ClaimServiceTests.cs

sed -i '' 's/_mapperMock.Object,/_mapperMock.Object, new Mock<IOptions<InsuranceClaimSystem.Infrastructure.Configuration.FileStorageSettings>>().Object,/g' UnitTests/Services/DocumentServiceTests.cs

sed -i '' 's/_unitOfWorkMock.Object,/_unitOfWorkMock.Object, new Mock<Hangfire.IBackgroundJobClient>().Object,/g' UnitTests/Services/NotificationServiceTests.cs

sed -i '' 's/_mapperMock.Object,/_mapperMock.Object, _loggerMock.Object, new Mock<INotificationService>().Object, new Mock<INomineeRepository>().Object/g' UnitTests/Services/PolicyServiceTests.cs
sed -i '' 's/ApplyForPolicyRequest/ApplyForPolicyDto/g' UnitTests/Services/PolicyServiceTests.cs
sed -i '' '/EndDate = /d' UnitTests/Services/PolicyServiceTests.cs

sed -i '' 's/_unitOfWorkMock.Object,/_unitOfWorkMock.Object, new Mock<INotificationService>().Object,/g' UnitTests/Services/PaymentServiceTests.cs
