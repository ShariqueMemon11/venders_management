using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Vendors.Application.Vendors.Dtos;
using Vendors.Domain.Enums;
using Vendors.Tests.Infrastructure;

namespace Vendors.Tests.Integration;

/// <summary>
/// End-to-end workflow through VendorService + real WorkflowEngineService on InMemory EF.
/// Covers submit → approve (to Active), submit → reject (back to Draft), and SoD on final approval.
/// Read these carefully — shallow green tests here would give false confidence.
/// </summary>
public class VendorWorkflowIntegrationTests
{
    [Fact]
    public async Task Submit_RequiresAtLeastOneChildDetail()
    {
        await using var host = await VendorTestHost.CreateAsync();
        using var scope = host.CreateScope();
        var sut = host.GetVendorService(scope);

        var created = await sut.CreateAsync(new VendorDto
        {
            LegalName = "Bare Draft",
            CurrencyCode = "USD"
        }, host.TenantId);

        var submit = await sut.SubmitVendorForApprovalAsync(
            created.Data,
            host.CurrentUser.UserId!,
            host.TenantId);

        submit.Success.Should().BeFalse();
        submit.Message.Should().Contain("contact");
    }

    [Fact]
    public async Task Submit_OnlyAllowsDraftStatus()
    {
        await using var host = await VendorTestHost.CreateAsync();

        Guid vendorId;
        using (var scope = host.CreateScope())
        {
            var sut = host.GetVendorService(scope);
            var created = await sut.CreateAsync(new VendorDto
            {
                LegalName = "Already Pending",
                CurrencyCode = "USD"
            }, host.TenantId);
            vendorId = created.Data;

            await sut.AddContactAsync(new VendorContactDto
            {
                VendorId = vendorId,
                Name = "Contact",
                Email = "c@example.com",
                ContactType = ContactType.Sales,
                IsPrimary = true,
                IsActive = true
            }, host.TenantId);
        }

        // Mutate in a separate context so Submit cannot see a stale tracked Draft.
        using (var mutate = host.CreateScope())
        {
            var db = host.GetDb(mutate);
            var v = await db.Vendors.SingleAsync(x => x.Id == vendorId);
            v.Status = VendorStatus.Active;
            await db.SaveChangesAsync();
        }

        using (var scope = host.CreateScope())
        {
            var sut = host.GetVendorService(scope);
            var submit = await sut.SubmitVendorForApprovalAsync(
                vendorId,
                host.CurrentUser.UserId!,
                host.TenantId);

            submit.Success.Should().BeFalse();
            submit.Message.Should().Contain("Draft");
        }
    }

    [Fact]
    public async Task Submit_Then_RiskApprove_Then_OtherProcurementApprove_ActivatesVendor()
    {
        await using var host = await VendorTestHost.CreateAsync();

        Guid vendorId;
        Guid workflowId;

        // --- Submit as procurement user A (becomes WorkflowInstance.CreatedBy) ---
        host.CurrentUser.As("procurement@example.com", "ProcurementManager", host.TenantId);
        using (var scope = host.CreateScope())
        {
            var sut = host.GetVendorService(scope);
            var created = await sut.CreateAsync(new VendorDto
            {
                LegalName = "Happy Path Vendor",
                CurrencyCode = "USD"
            }, host.TenantId);
            vendorId = created.Data;

            await sut.AddContactAsync(new VendorContactDto
            {
                VendorId = vendorId,
                Name = "Primary",
                Email = "p@example.com",
                ContactType = ContactType.Sales,
                IsPrimary = true,
                IsActive = true
            }, host.TenantId);

            var submit = await sut.SubmitVendorForApprovalAsync(
                vendorId,
                host.CurrentUser.UserId!,
                host.TenantId);

            submit.Success.Should().BeTrue(submit.Message);
            workflowId = submit.Data;
        }

        using (var scope = host.CreateScope())
        {
            var db = host.GetDb(scope);
            var vendor = await db.Vendors.SingleAsync(v => v.Id == vendorId);
            vendor.Status.Should().Be(VendorStatus.PendingReview);

            var instance = await db.WorkflowInstances.SingleAsync(w => w.Id == workflowId);
            instance.CreatedBy.Should().Be("procurement@example.com");
        }

        // --- Step 1: RiskAndCompliance approves ---
        host.CurrentUser.As("risk@example.com", "RiskAndCompliance", host.TenantId);
        using (var scope = host.CreateScope())
        {
            var sut = host.GetVendorService(scope);
            var riskApprove = await sut.ProcessWorkflowActionAsync(
                workflowId, "Approve", host.CurrentUser.UserId!, "risk ok", host.TenantId);

            riskApprove.Success.Should().BeTrue(riskApprove.Message);
        }

        using (var scope = host.CreateScope())
        {
            var db = host.GetDb(scope);
            var vendor = await db.Vendors.SingleAsync(v => v.Id == vendorId);
            vendor.Status.Should().Be(VendorStatus.PendingReview, "only final approval activates");
        }

        // --- Step 2 (final): a *different* procurement user approves (SoD OK) ---
        host.CurrentUser.As("procurement-other@example.com", "ProcurementManager", host.TenantId);
        using (var scope = host.CreateScope())
        {
            var sut = host.GetVendorService(scope);
            var finalApprove = await sut.ProcessWorkflowActionAsync(
                workflowId, "Approve", host.CurrentUser.UserId!, "final ok", host.TenantId);

            finalApprove.Success.Should().BeTrue(finalApprove.Message);
        }

        using (var scope = host.CreateScope())
        {
            var db = host.GetDb(scope);
            var vendor = await db.Vendors.SingleAsync(v => v.Id == vendorId);
            vendor.Status.Should().Be(VendorStatus.Active);

            var instance = await db.WorkflowInstances.SingleAsync(w => w.Id == workflowId);
            instance.CurrentState.Should().Be(Platform.Workflow.Enums.WorkflowState.Approved);
        }
    }

    [Fact]
    public async Task Submit_Then_Reject_ReturnsVendorToDraft()
    {
        await using var host = await VendorTestHost.CreateAsync();

        Guid vendorId;
        Guid workflowId;

        host.CurrentUser.As("procurement@example.com", "ProcurementManager", host.TenantId);
        using (var scope = host.CreateScope())
        {
            var sut = host.GetVendorService(scope);
            var created = await sut.CreateAsync(new VendorDto
            {
                LegalName = "Reject Path Vendor",
                CurrencyCode = "USD"
            }, host.TenantId);
            vendorId = created.Data;

            await sut.AddContactAsync(new VendorContactDto
            {
                VendorId = vendorId,
                Name = "Primary",
                Email = "r@example.com",
                ContactType = ContactType.Sales,
                IsPrimary = true,
                IsActive = true
            }, host.TenantId);

            var submit = await sut.SubmitVendorForApprovalAsync(
                vendorId, host.CurrentUser.UserId!, host.TenantId);
            submit.Success.Should().BeTrue(submit.Message);
            workflowId = submit.Data;
        }

        host.CurrentUser.As("risk@example.com", "RiskAndCompliance", host.TenantId);
        using (var scope = host.CreateScope())
        {
            var sut = host.GetVendorService(scope);
            var reject = await sut.ProcessWorkflowActionAsync(
                workflowId, "Reject", host.CurrentUser.UserId!, "insufficient docs", host.TenantId);

            reject.Success.Should().BeTrue(reject.Message);
        }

        using (var scope = host.CreateScope())
        {
            var db = host.GetDb(scope);
            var vendor = await db.Vendors.SingleAsync(v => v.Id == vendorId);
            vendor.Status.Should().Be(VendorStatus.Draft);

            var instance = await db.WorkflowInstances.SingleAsync(w => w.Id == workflowId);
            instance.CurrentState.Should().Be(Platform.Workflow.Enums.WorkflowState.Rejected);
        }
    }

    [Fact]
    public async Task Reject_Then_Resubmit_StartsFreshWorkflow_AndCanReachActive()
    {
        await using var host = await VendorTestHost.CreateAsync();

        Guid vendorId;
        Guid firstWorkflowId;
        Guid secondWorkflowId;

        const string submitter = "procurement@example.com";
        host.CurrentUser.As(submitter, "ProcurementManager", host.TenantId);
        using (var scope = host.CreateScope())
        {
            var sut = host.GetVendorService(scope);
            var created = await sut.CreateAsync(new VendorDto
            {
                LegalName = "Resubmit Vendor",
                CurrencyCode = "USD"
            }, host.TenantId);
            vendorId = created.Data;

            await sut.AddContactAsync(new VendorContactDto
            {
                VendorId = vendorId,
                Name = "Primary",
                Email = "resubmit@example.com",
                ContactType = ContactType.Sales,
                IsPrimary = true,
                IsActive = true
            }, host.TenantId);

            var submit1 = await sut.SubmitVendorForApprovalAsync(vendorId, submitter, host.TenantId);
            submit1.Success.Should().BeTrue(submit1.Message);
            firstWorkflowId = submit1.Data;
        }

        host.CurrentUser.As("risk@example.com", "RiskAndCompliance", host.TenantId);
        using (var scope = host.CreateScope())
        {
            var sut = host.GetVendorService(scope);
            var reject = await sut.ProcessWorkflowActionAsync(
                firstWorkflowId, "Reject", "risk@example.com", "fix and resubmit", host.TenantId);
            reject.Success.Should().BeTrue(reject.Message);
        }

        using (var scope = host.CreateScope())
        {
            var db = host.GetDb(scope);
            var vendor = await db.Vendors.SingleAsync(v => v.Id == vendorId);
            vendor.Status.Should().Be(VendorStatus.Draft);
        }

        // Resubmit after reject — rejected instance must not block a new workflow.
        host.CurrentUser.As(submitter, "ProcurementManager", host.TenantId);
        using (var scope = host.CreateScope())
        {
            var sut = host.GetVendorService(scope);
            var submit2 = await sut.SubmitVendorForApprovalAsync(vendorId, submitter, host.TenantId);
            submit2.Success.Should().BeTrue(submit2.Message);
            secondWorkflowId = submit2.Data;
            secondWorkflowId.Should().NotBe(firstWorkflowId);
        }

        using (var scope = host.CreateScope())
        {
            var db = host.GetDb(scope);
            var vendor = await db.Vendors.SingleAsync(v => v.Id == vendorId);
            vendor.Status.Should().Be(VendorStatus.PendingReview);

            var first = await db.WorkflowInstances.SingleAsync(w => w.Id == firstWorkflowId);
            first.CurrentState.Should().Be(Platform.Workflow.Enums.WorkflowState.Rejected);

            var second = await db.WorkflowInstances.SingleAsync(w => w.Id == secondWorkflowId);
            second.CurrentState.Should().Be(Platform.Workflow.Enums.WorkflowState.InProgress);
        }

        // Drive second attempt to Active with a different final approver (SoD OK).
        host.CurrentUser.As("risk@example.com", "RiskAndCompliance", host.TenantId);
        using (var scope = host.CreateScope())
        {
            var sut = host.GetVendorService(scope);
            (await sut.ProcessWorkflowActionAsync(
                secondWorkflowId, "Approve", "risk@example.com", "ok 2nd pass", host.TenantId))
                .Success.Should().BeTrue();
        }

        host.CurrentUser.As("procurement-other@example.com", "ProcurementManager", host.TenantId);
        using (var scope = host.CreateScope())
        {
            var sut = host.GetVendorService(scope);
            (await sut.ProcessWorkflowActionAsync(
                secondWorkflowId, "Approve", "procurement-other@example.com", "final 2nd", host.TenantId))
                .Success.Should().BeTrue();
        }

        using (var scope = host.CreateScope())
        {
            var db = host.GetDb(scope);
            var vendor = await db.Vendors.SingleAsync(v => v.Id == vendorId);
            vendor.Status.Should().Be(VendorStatus.Active);

            var second = await db.WorkflowInstances.SingleAsync(w => w.Id == secondWorkflowId);
            second.CurrentState.Should().Be(Platform.Workflow.Enums.WorkflowState.Approved);
        }
    }

    [Fact]
    public async Task FinalApprove_BySameSubmitter_FailsSeparationOfDuties()
    {
        await using var host = await VendorTestHost.CreateAsync();

        const string submitter = "procurement@example.com";
        Guid vendorId;
        Guid workflowId;

        host.CurrentUser.As(submitter, "ProcurementManager", host.TenantId);
        using (var scope = host.CreateScope())
        {
            var sut = host.GetVendorService(scope);
            var created = await sut.CreateAsync(new VendorDto
            {
                LegalName = "SoD Vendor",
                CurrencyCode = "USD"
            }, host.TenantId);
            vendorId = created.Data;

            await sut.AddContactAsync(new VendorContactDto
            {
                VendorId = vendorId,
                Name = "Primary",
                Email = "sod@example.com",
                ContactType = ContactType.Sales,
                IsPrimary = true,
                IsActive = true
            }, host.TenantId);

            var submit = await sut.SubmitVendorForApprovalAsync(vendorId, submitter, host.TenantId);
            submit.Success.Should().BeTrue(submit.Message);
            workflowId = submit.Data;
        }

        // Risk clears step 1
        host.CurrentUser.As("risk@example.com", "RiskAndCompliance", host.TenantId);
        using (var scope = host.CreateScope())
        {
            var sut = host.GetVendorService(scope);
            var riskApprove = await sut.ProcessWorkflowActionAsync(
                workflowId, "Approve", "risk@example.com", "risk ok", host.TenantId);
            riskApprove.Success.Should().BeTrue(riskApprove.Message);
        }

        // Same submitter attempts final approval — must fail SoD
        host.CurrentUser.As(submitter, "ProcurementManager", host.TenantId);
        using (var scope = host.CreateScope())
        {
            var sut = host.GetVendorService(scope);
            var sod = await sut.ProcessWorkflowActionAsync(
                workflowId, "Approve", submitter, "I approve my own request", host.TenantId);

            sod.Success.Should().BeFalse();
            sod.Message.Should().Contain("Separation of duties");
        }

        using (var scope = host.CreateScope())
        {
            var db = host.GetDb(scope);
            var vendor = await db.Vendors.SingleAsync(v => v.Id == vendorId);
            vendor.Status.Should().Be(VendorStatus.PendingReview, "SoD failure must not activate the vendor");
        }
    }

    [Fact]
    public async Task Approve_WithWrongRole_IsRejected_BeforeSoD()
    {
        await using var host = await VendorTestHost.CreateAsync();

        Guid workflowId;
        host.CurrentUser.As("procurement@example.com", "ProcurementManager", host.TenantId);
        using (var scope = host.CreateScope())
        {
            var sut = host.GetVendorService(scope);
            var created = await sut.CreateAsync(new VendorDto
            {
                LegalName = "Wrong Role Vendor",
                CurrencyCode = "USD"
            }, host.TenantId);

            await sut.AddContactAsync(new VendorContactDto
            {
                VendorId = created.Data,
                Name = "Primary",
                Email = "wr@example.com",
                ContactType = ContactType.Sales,
                IsPrimary = true,
                IsActive = true
            }, host.TenantId);

            var submit = await sut.SubmitVendorForApprovalAsync(
                created.Data, host.CurrentUser.UserId!, host.TenantId);
            submit.Success.Should().BeTrue(submit.Message);
            workflowId = submit.Data;
        }

        // First step requires RiskAndCompliance — ProcurementManager must not act
        host.CurrentUser.As("procurement@example.com", "ProcurementManager", host.TenantId);
        using (var scope = host.CreateScope())
        {
            var sut = host.GetVendorService(scope);
            var wrongRole = await sut.ProcessWorkflowActionAsync(
                workflowId, "Approve", host.CurrentUser.UserId!, "skip risk", host.TenantId);

            wrongRole.Success.Should().BeFalse();
            wrongRole.Message.Should().Contain("RiskAndCompliance");
        }
    }
}
