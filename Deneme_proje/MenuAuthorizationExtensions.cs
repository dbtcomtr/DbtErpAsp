using System.Data.SqlClient;

public static class MenuAuthorizationExtensions
{
    public static bool HasMenuAccess(this IConfiguration configuration, string userNo, string menuUrl)
    {
        var connectionString = configuration.GetConnectionString("ERPDatabase");

        using (var connection = new SqlConnection(connectionString))
        {
            connection.Open();
            var query = "SELECT Yetkililer FROM Menu WHERE MenuUrl = @MenuUrl AND Durum = 1";

            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@MenuUrl", menuUrl);
                var result = command.ExecuteScalar()?.ToString();

                if (string.IsNullOrEmpty(result))
                    return false;

                var authorizedUsers = result.Split(',');
                return authorizedUsers.Contains(userNo);
            }
        }
    }
}