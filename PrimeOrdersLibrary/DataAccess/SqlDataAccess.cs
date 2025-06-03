using System.Data;
using System.Diagnostics.CodeAnalysis;

using Dapper;

using Microsoft.Data.SqlClient;

namespace PrimeOrdersLibrary.DataAccess;

public static class SqlDataAccess
{
	public static async Task<List<T>> LoadData<T, U>(string storedProcedure, U parameters)
	{
		using IDbConnection connection = new SqlConnection(ConnectionStrings.Local);

		List<T> rows = [.. await connection.QueryAsync<T>(storedProcedure, parameters, commandType: CommandType.StoredProcedure)];

		return rows;
	}

	public static async Task SaveData<T>(string storedProcedure, T parameters)
	{
		using IDbConnection connection = new SqlConnection(ConnectionStrings.Local);

		await connection.ExecuteAsync(storedProcedure, parameters, commandType: CommandType.StoredProcedure);
	}
}

public class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
{
	public override DateOnly Parse(object value)
	{
		return value is DateOnly dateOnly
			? dateOnly
			: DateOnly.FromDateTime((DateTime)value);
	}

	public override void SetValue([DisallowNull] IDbDataParameter parameter, DateOnly value)
	{
		parameter.Value = value.ToDateTime(TimeOnly.MinValue);
		parameter.DbType = DbType.Date;
	}
}
