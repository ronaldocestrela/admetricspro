using Bunit;
using FluentAssertions;
using UnitTests.Frontend.Common;
using WebApp.Components.Pages.Reports;

namespace UnitTests.Frontend.Components.Reports;

/// <summary>Garante identificação acessível do diálogo de agendamento.</summary>
public sealed class ReportModalAccessibilityTests : BunitTestBase
{
    /// <summary>O formulário aberto expõe um diálogo modal identificado pelo título.</summary>
    [Fact]
    public void Schedule_WhenOpen_HasAccessibleDialogTitle()
    {
        var cut = Render<ReportScheduleModal>(p => p.Add(x => x.IsOpen, true));
        var dialog = cut.Find("[role='dialog']");
        dialog.GetAttribute("aria-modal").Should().Be("true");
        var titleId = dialog.GetAttribute("aria-labelledby");
        titleId.Should().NotBeNullOrEmpty();
        cut.Find($"#{titleId}").TextContent.Should().Contain("Agendamento");
    }
}
