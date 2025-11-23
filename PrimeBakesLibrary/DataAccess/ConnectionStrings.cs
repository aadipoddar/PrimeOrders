namespace PrimeBakesLibrary.DataAccess;

public static class ConnectionStrings
{
	public static string Azure => $"Server=primeorders.database.windows.net,1433;Initial Catalog={Secrets.DatabaseName};Persist Security Info=False;User ID={Secrets.DatabaseUserId};Password={Secrets.DatabasePassword};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=True;Connection Timeout=300;";
	public static string Local => $"Data Source=AADILAPI;Initial Catalog={Secrets.DatabaseName};Integrated Security=True;Connect Timeout=300;Encrypt=True;Trust Server Certificate=True;Application Intent=ReadWrite;Multi Subnet Failover=False";
}
