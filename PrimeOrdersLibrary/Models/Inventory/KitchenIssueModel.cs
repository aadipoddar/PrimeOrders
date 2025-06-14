﻿namespace PrimeOrdersLibrary.Models.Inventory;

public class KitchenIssueModel
{
	public int Id { get; set; }
	public int KitchenId { get; set; }
	public int LocationId { get; set; }
	public int UserId { get; set; }
	public string TransactionNo { get; set; }
	public DateTime IssueDate { get; set; }
	public bool Status { get; set; }
}

public class KitchenIssueDetailModel
{
	public int Id { get; set; }
	public int KitchenIssueId { get; set; }
	public int RawMaterialId { get; set; }
	public decimal Quantity { get; set; }
	public bool Status { get; set; }
}

public class KitchenIssueOverviewModel
{
	public int KitchenIssueId { get; set; }
	public string TransactionNo { get; set; }
	public int KitchenId { get; set; }
	public string KitchenName { get; set; }
	public DateTime IssueDate { get; set; }
	public int UserId { get; set; }
	public string UserName { get; set; }
	public int TotalProducts { get; set; }
	public decimal TotalQuantity { get; set; }
	public bool Status { get; set; }
}