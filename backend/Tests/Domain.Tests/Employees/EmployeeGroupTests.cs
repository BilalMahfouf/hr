//using Modules.Employees.Domain.EmployeeGroups;
//using Modules.Employees.Domain.EmployeeGroups.WorkSchedules;
//using Modules.Shared.Domain.Common;

//namespace Domain.Tests.Employees;

//public sealed class EmployeeGroupTests
//{
//    private static EmployeeGroup CreateGroup() =>
//        EmployeeGroup.Create("Day Shift", 2, isSecurity: false);

//    [Fact]
//    public void Create_SetsExpectedInitialState()
//    {
//        var group = CreateGroup();

//        Assert.NotEqual(Guid.Empty, group.Id.Value);
//        Assert.Equal("Day Shift", group.Name);
//        Assert.Equal(2, group.NumberOfRotations);
//        Assert.False(group.IsSecurity);
//        Assert.Null(group.Description);
//        Assert.Empty(group.WorkSchedules);
//    }

//    [Fact]
//    public void Create_WithDescription_SetsDescription()
//    {
//        var group = EmployeeGroup.Create("Night Shift", 3, isSecurity: true, "Security group");

//        Assert.Equal("Night Shift", group.Name);
//        Assert.Equal(3, group.NumberOfRotations);
//        Assert.True(group.IsSecurity);
//        Assert.Equal("Security group", group.Description);
//    }

//    [Theory]
//    [InlineData("")]
//    [InlineData("   ")]
//    public void Create_WhenNameNullOrWhitespace_ThrowsDomainException(string? name)
//    {
//        var exception = Assert.Throws<DomainException>(() =>
//            EmployeeGroup.Create(name!, 1, isSecurity: false));

//        Assert.Equal(EmployeeGroupErrors.InvalidName.Code, exception.Error.Code);
//    }

//    [Fact]
//    public void Create_WhenNameNull_ThrowsDomainException()
//    {
//        var exception = Assert.Throws<DomainException>(() =>
//            EmployeeGroup.Create(null!, 1, isSecurity: false));

//        Assert.Equal(EmployeeGroupErrors.InvalidName.Code, exception.Error.Code);
//    }

//    [Fact]
//    public void Create_WhenNumberOfRotationsZero_ThrowsDomainException()
//    {
//        var exception = Assert.Throws<DomainException>(() =>
//            EmployeeGroup.Create("Day Shift", 0, isSecurity: false));

//        Assert.Equal(EmployeeGroupErrors.InvalidNumberOfRotations.Code, exception.Error.Code);
//    }

//    [Fact]
//    public void AddWorkSchedule_AddsToCollection()
//    {
//        var group = CreateGroup();
//        var schedule = WorkSchedule.Create(
//            group.Id,
//            new TimeOnly(8, 0),
//            new TimeOnly(16, 0),
//            new TimeOnly(12, 0),
//            new TimeOnly(13, 0),
//            allowedCheckInLatenessMinutes: 5,
//            allowedCheckOutEarlinessMinutes: 5);

//        group.AddWorkSchedule(schedule);

//        Assert.Single(group.WorkSchedules);
//        Assert.Equal(group.Id, group.WorkSchedules.Single().EmployeeGroupId);
//    }

//    [Fact]
//    public void AddWorkSchedule_WhenScheduleBelongsToAnotherGroup_ThrowsDomainException()
//    {
//        var group = CreateGroup();
//        var otherGroup = EmployeeGroup.Create("Night Shift", 1, isSecurity: false);
//        var schedule = WorkSchedule.Create(
//            otherGroup.Id,
//            new TimeOnly(8, 0),
//            new TimeOnly(16, 0),
//            new TimeOnly(12, 0),
//            new TimeOnly(13, 0),
//            allowedCheckInLatenessMinutes: 0,
//            allowedCheckOutEarlinessMinutes: 0);

//        var exception = Assert.Throws<DomainException>(() => group.AddWorkSchedule(schedule));

//        Assert.Equal(EmployeeGroupErrors.WorkScheduleBelongsToAnotherGroup.Code, exception.Error.Code);
//        Assert.Empty(group.WorkSchedules);
//    }
//}