using PrimeBakes.Shared.Services;

namespace PrimeBakes.Web.Services;

public class FormFactor : IFormFactor
{
    public string GetFormFactor() =>
        "Web";

    public string GetPlatform() =>
        Environment.OSVersion.ToString();
}
