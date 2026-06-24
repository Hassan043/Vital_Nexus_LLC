namespace VitalNexus.Infrastructure.Accounts;

public static class OnboardingValidation
{
    public static void ValidateCustomerDisplayName(string customerDisplayName)
    {
        if (string.IsNullOrWhiteSpace(customerDisplayName))
        {
            throw new InvalidOperationException("Customer name is required.");
        }

        if (customerDisplayName.Trim().Length < 2)
        {
            throw new InvalidOperationException("Customer name must be at least 2 characters.");
        }
    }

    public static void ValidateClinicName(string clinicName)
    {
        if (string.IsNullOrWhiteSpace(clinicName))
        {
            throw new InvalidOperationException("Clinic name is required.");
        }

        if (clinicName.Trim().Length < 2)
        {
            throw new InvalidOperationException("Clinic name must be at least 2 characters.");
        }
    }

    public static void ValidateClinicProfile(string? contactEmail, string? phone, string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(contactEmail))
        {
            throw new InvalidOperationException("Clinic contact email is required.");
        }

        if (!contactEmail.Contains('@', StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Clinic contact email is invalid.");
        }

        if (string.IsNullOrWhiteSpace(phone))
        {
            throw new InvalidOperationException("Clinic phone is required.");
        }

        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            throw new InvalidOperationException("Clinic time zone is required.");
        }
    }
}
