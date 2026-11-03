using Anarchy.Toolkit.Web.Ui.Infrastructure.Service.Http.Client.Exceptions;
using Anarchy.Toolkit.Web.Ui.Ui.Presentation.Service;
using AwesomeAssertions;
using System.Net;

namespace Anarchy.Toolkit.Tests.Unit.Web.Ui.Ui.Presentation.Service;

public class NotificationServiceTest
{
    [Fact]
    public void Notify_StacksNewestFirstAndRaisesChanged()
    {
        // Arrange
        using var service = new NotificationService();
        var changes = 0;
        service.Changed += () => changes++;

        // Act
        service.Info("first");
        service.Success("second", "  ");

        // Assert
        service.Notifications.Select(x => x.Message).Should().Equal("second", "first");
        service.Notifications[0].Level.Should().Be(NotificationLevel.Success);
        service.Notifications[0].Details.Should().BeNull();
        changes.Should().Be(2);
    }

    [Fact]
    public void Notify_BlankMessage_IsIgnored()
    {
        using var service = new NotificationService();

        service.Warning(" ");

        service.Notifications.Should().BeEmpty();
    }

    [Fact]
    public async Task Error_DownstreamException_UsesResponseContentAsDetails()
    {
        // Arrange
        using var service = new NotificationService();
        var exception = await DownstreamHttpRequestException.CreateAsync(new HttpResponseMessage(HttpStatusCode.Conflict) {
            Content = new StringContent("conflicting state")
        }, TestContext.Current.CancellationToken);

        // Act
        service.Error(exception);

        // Assert
        service.Notifications.Should().ContainSingle()
            .Which.Should().Match<Notification>(x =>
                x.Level == NotificationLevel.Error && x.Details == "conflicting state");
    }

    [Fact]
    public void Error_OtherException_UsesInnerExceptionAsDetails()
    {
        using var service = new NotificationService();

        service.Error(new InvalidOperationException("outer", new TimeoutException("inner")));

        service.Notifications[0].Message.Should().Be("outer");
        service.Notifications[0].Details.Should().Contain("inner");
    }

    [Fact]
    public async Task Dismiss_FlagsThenRemovesNotification()
    {
        // Arrange
        using var service = new NotificationService();
        service.Info("bye");
        var id = service.Notifications[0].Id;

        // Act
        service.Dismiss(id);

        // Assert
        service.Notifications[0].IsDismissing.Should().BeTrue();
        await Task.Delay(NotificationService.FadeOutDuration * 3, TestContext.Current.CancellationToken);
        service.Notifications.Should().BeEmpty();
    }
}
