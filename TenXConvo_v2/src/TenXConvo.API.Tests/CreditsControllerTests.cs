using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TenXConvo.API.Controllers;
using TenXConvo.Infrastructure.Services;
using TenXConvo.Infrastructure.Data;
using Microsoft.Extensions.Logging;
using Xunit;
using Microsoft.EntityFrameworkCore;

namespace TenXConvo.API.Tests;

public class CreditsControllerTests
{
    [Fact]
    public async Task GetBalance_ReturnsOk_WithCredits()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expectedCredits = new CreditInfo(100, 5.0, 2.0, 10, 3);

        // We use Moq as explicitly requested
        // Passing nulls to the DbContext and Logger dependencies since we mock the methods
        var mockService = new Mock<CreditService>(MockBehavior.Strict, new object[] { null!, null! });
        mockService.Setup(s => s.GetCreditsAsync(userId)).ReturnsAsync(expectedCredits);

        var controller = new CreditsController(mockService.Object);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim("sub", userId.ToString())
        }, "mock"));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        // Act
        var result = await controller.GetBalance();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;

        var successProp = response?.GetType().GetProperty("success");
        var dataProp = response?.GetType().GetProperty("data");

        Assert.NotNull(successProp);
        Assert.NotNull(dataProp);

        var successValue = successProp?.GetValue(response);
        Assert.True(successValue as bool?);

        var dataValue = dataProp?.GetValue(response) as CreditInfo;
        Assert.NotNull(dataValue);
        Assert.Equal(100, dataValue.TextCharsRemaining);
        Assert.Equal(5.0, dataValue.AudioMinsRemaining);
    }
}
