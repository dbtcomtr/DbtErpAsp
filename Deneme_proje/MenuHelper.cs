using Microsoft.Data.SqlClient;
using Dapper;

public static class MenuHelper
{
    private static readonly IConfiguration _configuration;

    static MenuHelper()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json");
        _configuration = builder.Build();
    }

    public static bool KullaniciYetkisiVarMi(string controllerAdi, string actionAdi, string userNo)
    {
        try
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("ERPDatabase")))
            {
                connection.Open();
                var query = @"SELECT COUNT(1) 
                    FROM MenuYonetim 
                    WHERE LOWER(ControllerAdi) = LOWER(@controller)
                    AND LOWER(ActionAdi) = LOWER(@action)
                    AND Yetki IS NOT NULL 
                    AND Yetki != ''
                    AND (
                        Yetki = @userNo OR 
                        Yetki LIKE @userNoStart OR 
                        Yetki LIKE @userNoMiddle OR 
                        Yetki LIKE @userNoEnd
                    )";

                var yetki = connection.ExecuteScalar<int>(query, new
                {
                    controller = controllerAdi,
                    action = actionAdi,
                    userNo = userNo,
                    userNoStart = $"{userNo},%",
                    userNoMiddle = $"%,{userNo},%",
                    userNoEnd = $"%,{userNo}"
                });

                return yetki > 0;
            }
        }
        catch
        {
            return false;
        }
    }
}