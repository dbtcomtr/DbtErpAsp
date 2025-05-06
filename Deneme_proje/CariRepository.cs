using Microsoft.Data.SqlClient; // SqlConnection için yeni ad alanı
using System.Data;
using Microsoft.Extensions.Configuration;
public class CariRepository
{
    private readonly string _connectionString;

    public CariRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("MikroDB");
    }

    public DataTable GetCarilerAlacak(int upbPb, string sektorKodu, string bolgeKodu, string grupKodu, string temsilciKodu)
    {
        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            using (SqlCommand command = new SqlCommand("dbo.DBT_CarileriGetir_Alacak", connection))
            {
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.AddWithValue("@UPB_PB", upbPb);
                command.Parameters.AddWithValue("@SektorKodu", (object)sektorKodu ?? DBNull.Value);
                command.Parameters.AddWithValue("@BolgeKodu", (object)bolgeKodu ?? DBNull.Value);
                command.Parameters.AddWithValue("@GrupKodu", (object)grupKodu ?? DBNull.Value);
                command.Parameters.AddWithValue("@TemsilciKodu", (object)temsilciKodu ?? DBNull.Value);

                SqlDataAdapter adapter = new SqlDataAdapter(command);
                DataTable resultTable = new DataTable();
                adapter.Fill(resultTable);

                return resultTable;
            }
        }
    }
    public DataTable GetCarilerVerecek(int upbPb, string sektorKodu, string bolgeKodu, string grupKodu, string temsilciKodu)
    {
        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            using (SqlCommand command = new SqlCommand("dbo.DBT_CarileriGetir_Verecek", connection))
            {
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.AddWithValue("@UPB_PB", upbPb);
                command.Parameters.AddWithValue("@SektorKodu", (object)sektorKodu ?? DBNull.Value);
                command.Parameters.AddWithValue("@BolgeKodu", (object)bolgeKodu ?? DBNull.Value);
                command.Parameters.AddWithValue("@GrupKodu", (object)grupKodu ?? DBNull.Value);
                command.Parameters.AddWithValue("@TemsilciKodu", (object)temsilciKodu ?? DBNull.Value);

                SqlDataAdapter adapter = new SqlDataAdapter(command);
                DataTable resultTable = new DataTable();
                adapter.Fill(resultTable);

                return resultTable;
            }
        }
    }

}