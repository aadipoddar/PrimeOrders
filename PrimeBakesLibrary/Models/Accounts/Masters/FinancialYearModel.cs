namespace PrimeBakesLibrary.Models.Accounts.Masters;

public class FinancialYearModel
{
	public int Id { get; set; }
	public DateOnly StartDate { get; set; }
	public DateOnly EndDate { get; set; }
	public int YearNo { get; set; }
	public string Remarks { get; set; }
	public bool Status { get; set; }
}
