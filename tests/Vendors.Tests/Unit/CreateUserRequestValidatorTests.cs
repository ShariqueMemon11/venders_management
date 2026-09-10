using FluentValidation.TestHelper;
using Shared.Application.Identity;
using Vendors.Application.Users.Validators;

namespace Vendors.Tests.Unit;

public class CreateUserRequestValidatorTests
{
    private readonly CreateUserRequestValidator _validator = new();

    [Fact]
    public void RejectsAdminRole()
    {
        var result = _validator.TestValidate(new CreateUserRequest
        {
            DisplayName = "X",
            Email = "x@example.com",
            Role = "Admin",
            Password = "password1"
        });

        result.ShouldHaveValidationErrorFor(x => x.Role)
            .WithErrorMessage("Role must be ProcurementManager, RiskAndCompliance, or Viewer.");
    }
}
