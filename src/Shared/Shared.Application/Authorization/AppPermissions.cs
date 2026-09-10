namespace Shared.Application.Authorization;

/// <summary>
/// Canonical permission catalog. Roles are mapped to these; APIs/UI should check permissions.
/// </summary>
public static class AppPermissions
{
    public const string ClaimType = "permission";

    public static class Vendor
    {
        public const string View = "Vendor.View";
        public const string Create = "Vendor.Create";
        public const string Edit = "Vendor.Edit";
        public const string Submit = "Vendor.Submit";
        public const string Terminate = "Vendor.Terminate";
        public const string ViewSensitive = "Vendor.ViewSensitive"; // tax, bank, contracts, docs
    }

    public static class Workflow
    {
        public const string View = "Workflow.View";
        public const string Approve = "Workflow.Approve";
        public const string Reject = "Workflow.Reject";
        public const string Start = "Workflow.Start";
    }

    public static class Compliance
    {
        public const string View = "Compliance.View";
        public const string Edit = "Compliance.Edit";
    }

    public static class Risk
    {
        public const string View = "Risk.View";
        public const string Edit = "Risk.Edit";
    }

    public static class Performance
    {
        public const string View = "Performance.View";
        public const string Edit = "Performance.Edit";
    }

    public static class Document
    {
        public const string Upload = "Document.Upload";
        public const string Download = "Document.Download";
        public const string Delete = "Document.Delete";
        public const string Verify = "Document.Verify";
    }

    public static class Contract
    {
        public const string View = "Contract.View";
        public const string Edit = "Contract.Edit";
    }

    public static class Report
    {
        public const string VendorMaster = "Report.VendorMaster";
        public const string Risk = "Report.Risk";
        public const string Performance = "Report.Performance";
    }

    public static class Administration
    {
        public const string ManageUsers = "Admin.Users";
        public const string ViewAudit = "Admin.Audit";
    }

    public static class User
    {
        /// <summary>Create, list, deactivate/activate, and terminate users. Admin only.</summary>
        public const string Manage = "User.Manage";
    }

    public static IReadOnlyDictionary<string, string[]> RoleMap { get; } = new Dictionary<string, string[]>
    {
        ["Admin"] = new[]
        {
            Vendor.View, Vendor.Create, Vendor.Edit, Vendor.Submit, Vendor.Terminate, Vendor.ViewSensitive,
            Workflow.View, Workflow.Approve, Workflow.Reject, Workflow.Start,
            Compliance.View, Compliance.Edit,
            Risk.View, Risk.Edit,
            Performance.View, Performance.Edit,
            Document.Upload, Document.Download, Document.Delete, Document.Verify,
            Contract.View, Contract.Edit,
            Report.VendorMaster, Report.Risk, Report.Performance,
            Administration.ManageUsers, Administration.ViewAudit,
            User.Manage,
            "Notification.View", "Notification.Manage"
        },
        ["ProcurementManager"] = new[]
        {
            Vendor.View, Vendor.Create, Vendor.Edit, Vendor.Submit, Vendor.ViewSensitive,
            Workflow.View, Workflow.Approve, Workflow.Reject, Workflow.Start,
            Performance.View, Performance.Edit,
            Document.Upload, Document.Download, Document.Delete,
            Contract.View, Contract.Edit,
            Compliance.View, Risk.View,
            Report.VendorMaster, Report.Performance,
            "Notification.View"
        },
        ["RiskAndCompliance"] = new[]
        {
            Vendor.View, Vendor.ViewSensitive,
            Workflow.View, Workflow.Approve, Workflow.Reject,
            Compliance.View, Compliance.Edit,
            Risk.View, Risk.Edit,
            Document.Verify, Document.Download,
            Performance.View,
            Report.Risk,
            "Notification.View"
        },
        ["Viewer"] = new[]
        {
            Vendor.View,
            Performance.View,
            "Notification.View"
        }
    };

    public static IEnumerable<string> ForRole(string role) =>
        RoleMap.TryGetValue(role, out var perms) ? perms : Array.Empty<string>();
}
