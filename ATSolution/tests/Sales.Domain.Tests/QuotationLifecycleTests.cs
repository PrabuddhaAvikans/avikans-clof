using Sales.Domain.Common;
using Sales.Domain.Quotations;

namespace Sales.Domain.Tests;

public class QuotationStatusesTests
{
    [Theory]
    [InlineData(QuotationStatuses.Sent)]
    [InlineData(QuotationStatuses.Viewed)]
    [InlineData(QuotationStatuses.CustomerFeedback)]
    [InlineData(QuotationStatuses.RevisionRequired)]
    [InlineData(QuotationStatuses.Revised)]
    [InlineData(QuotationStatuses.Accepted)]
    public void IsEditable_allows_post_send_statuses(string status)
    {
        Assert.True(QuotationStatuses.IsEditable(status));
    }

    [Theory]
    [InlineData(QuotationStatuses.Converted)]
    [InlineData(QuotationStatuses.Rejected)]
    [InlineData(QuotationStatuses.Expired)]
    public void IsEditable_blocks_terminal_statuses(string status)
    {
        Assert.False(QuotationStatuses.IsEditable(status));
    }

    [Theory]
    [InlineData(QuotationStatuses.Sent)]
    [InlineData(QuotationStatuses.Accepted)]
    [InlineData(QuotationStatuses.Converted)]
    [InlineData(QuotationStatuses.Rejected)]
    public void CanLogContact_is_independent_of_approval(string status)
    {
        Assert.True(QuotationStatuses.CanLogContact(status));
    }

    [Fact]
    public void ResolveStatusAfterContentSave_marks_issued_quotation_as_revised()
    {
        var next = QuotationStatuses.ResolveStatusAfterContentSave(QuotationStatuses.Sent, "save");
        Assert.Equal(QuotationStatuses.Revised, next);
    }

    [Fact]
    public void ResolveStatusAfterContentSave_marks_draft_revision_as_revision_required()
    {
        var next = QuotationStatuses.ResolveStatusAfterContentSave(QuotationStatuses.CustomerFeedback, "draft");
        Assert.Equal(QuotationStatuses.RevisionRequired, next);
    }

    [Fact]
    public void ResolveStatusAfterContentSave_promotes_draft_to_ready_to_send()
    {
        var next = QuotationStatuses.ResolveStatusAfterContentSave(QuotationStatuses.Draft, "save");
        Assert.Equal(QuotationStatuses.ReadyToSend, next);
    }

    [Fact]
    public void CanTransitionTo_allows_sent_to_accepted()
    {
        Assert.True(QuotationStatuses.CanTransitionTo(QuotationStatuses.Sent, QuotationStatuses.Accepted));
    }

    [Fact]
    public void CanTransitionTo_blocks_converted_changes()
    {
        Assert.False(QuotationStatuses.CanTransitionTo(QuotationStatuses.Converted, QuotationStatuses.Revised));
    }

    [Fact]
    public void CanConvert_includes_revised_and_accepted()
    {
        Assert.True(QuotationStatuses.CanConvert(QuotationStatuses.Accepted));
        Assert.True(QuotationStatuses.CanConvert(QuotationStatuses.Revised));
        Assert.True(QuotationStatuses.CanConvert(QuotationStatuses.Sent));
        Assert.False(QuotationStatuses.CanConvert(QuotationStatuses.CustomerFeedback));
    }
}

public class QuotationLifecycleTests
{
    private static Quotation CreateQuotation(string status)
    {
        var quotation = Quotation.Create(
            "QT-TEST-0001",
            Guid.NewGuid(),
            "Test Customer",
            "customer@example.com",
            QuotationStatuses.Draft,
            PriorityValues.Medium,
            DateTimeOffset.UtcNow.AddDays(14),
            notes: null,
            terms: null,
            discountAmount: 0,
            subtotal: 100,
            taxAmount: 0,
            totalAmount: 100,
            currency: SalesDefaults.Currency,
            paymentStatus: PaymentStatuses.Unpaid,
            billingAddressJson: "{}",
            shippingAddressJson: null,
            attachmentsJson: "[]",
            revisionsJson: "[]",
            workflowSnapshotJson: null,
            createdBy: "tester",
            createdByName: "Tester");

        quotation.ApplyStatus(status);
        return quotation;
    }

    [Fact]
    public void MarkAccepted_sets_accepted_timestamp()
    {
        var quotation = CreateQuotation(QuotationStatuses.Sent);
        quotation.MarkAccepted();

        Assert.Equal(QuotationStatuses.Accepted, quotation.Status);
        Assert.NotNull(quotation.AcceptedAtUtc);
    }

    [Fact]
    public void MarkRevised_after_accepted_keeps_prior_acceptance_timestamp()
    {
        var quotation = CreateQuotation(QuotationStatuses.Sent);
        quotation.MarkAccepted();
        var acceptedAt = quotation.AcceptedAtUtc;

        quotation.MarkRevised();

        Assert.Equal(QuotationStatuses.Revised, quotation.Status);
        Assert.Equal(acceptedAt, quotation.AcceptedAtUtc);
    }

    [Fact]
    public void MarkCustomerFeedback_from_sent_sets_feedback_status()
    {
        var quotation = CreateQuotation(QuotationStatuses.Sent);
        quotation.MarkCustomerFeedback();

        Assert.Equal(QuotationStatuses.CustomerFeedback, quotation.Status);
        Assert.NotNull(quotation.ViewedAtUtc);
    }

    [Fact]
    public void Contact_logging_does_not_require_status_change_for_accepted()
    {
        var quotation = CreateQuotation(QuotationStatuses.Accepted);
        var statusBefore = quotation.Status;
        var acceptedAt = quotation.AcceptedAtUtc;

        quotation.Touch();

        Assert.Equal(statusBefore, quotation.Status);
        Assert.Equal(acceptedAt, quotation.AcceptedAtUtc);
        Assert.True(QuotationStatuses.CanLogContact(quotation.Status));
    }
}
