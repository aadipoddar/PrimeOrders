namespace PrimeOrdersLibrary.DataAccess;

public static class TableNames
{
	public static string User => "User";
	public static string Location => "Location";
}

public static class StoredProcedureNames
{
	public static string LoadTableData => "Load_TableData";
	public static string LoadTableDataById => "Load_TableData_By_Id";
	public static string LoadTableDataByStatus => "Load_TableData_By_Status";

	public static string LoadUserByPasscode => "Load_User_By_Passcode";

	public static string InsertUser => "Insert_User";
	public static string InsertLocation => "Insert_Location";
}

public static class ViewNames
{
}