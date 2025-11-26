namespace PrimeBakesLibrary.Models.Common;

public class UserModel
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int Passcode { get; set; }
    public int LocationId { get; set; }
    public bool Sales { get; set; }
    public bool Order { get; set; }
    public bool Inventory { get; set; }
    public bool Accounts { get; set; }
    public bool Admin { get; set; }
    public string? Remarks { get; set; }
	public bool Status { get; set; }
}

public enum UserRoles
{
    Admin,
    Sales,
    Order,
    Inventory,
    Accounts
}

public record AuthenticationResult(
    bool IsAuthenticated,
    UserModel User = null,
    string ErrorMessage = null);