namespace ConsoleApp1;

using Npgsql;
using System;

public class ServElron
{
    private readonly string _connectionString;

    public ServElron(string connectionString)
    {
        _connectionString = connectionString;
    }

    public void InsertServElron(string reis, string liin, string reisiAlgusAeg, string reisiLoppAeg,
                                string kiirus, string latitude, string longitude, string rongiSuund,
                                string erinevusPlaanist, string lisateade, string pohjusTeade,
                                string avaldaKodulehel, string asukohaUuendus, string reisiStaatus,
                                string viimanePeatus)
    {
        int kiirusInt = int.Parse(kiirus);
        decimal latitudeDecimal = decimal.Parse(latitude);
        decimal longitudeDecimal = decimal.Parse(longitude);
        int rongiSuundInt = int.Parse(rongiSuund);
        DateTime asukohaUuendusDateTime = DateTime.Parse(asukohaUuendus);
        DateTime asukohaUuendusUtc = asukohaUuendusDateTime.ToUniversalTime();

        
        using var connection = new NpgsqlConnection(_connectionString);
        connection.Open();

        using var command = new NpgsqlCommand("INSERT INTO serv_elron (reis, liin, reisi_algus_aeg, " +
            "reisi_lopp_aeg, kiirus, latitude, longitude, rongi_suund, erinevus_plaanist, lisateade, " +
            "pohjus_teade, avalda_kodulehel, asukoha_uuendus, reisi_staatus, viimane_peatus) " +
            "VALUES (@reis, @liin, @reisiAlgusAeg, @reisiLoppAeg, @kiirus, @latitude, @longitude, " +
            "@rongiSuund, @erinevusPlaanist, @lisateade, @pohjusTeade, @avaldaKodulehel, " +
            "@asukohaUuendus, @reisiStaatus, @viimanePeatus)", connection);

        command.Parameters.AddWithValue("@reis", reis);
        command.Parameters.AddWithValue("@liin", liin);
        command.Parameters.AddWithValue("@reisiAlgusAeg", reisiAlgusAeg);
        command.Parameters.AddWithValue("@reisiLoppAeg", reisiLoppAeg);
        command.Parameters.AddWithValue("@kiirus", kiirusInt);
        command.Parameters.AddWithValue("@latitude", latitudeDecimal);
        command.Parameters.AddWithValue("@longitude", longitudeDecimal);
        command.Parameters.AddWithValue("@rongiSuund", rongiSuundInt);
        command.Parameters.AddWithValue("@erinevusPlaanist", erinevusPlaanist);
        command.Parameters.AddWithValue("@lisateade", lisateade);
        command.Parameters.AddWithValue("@pohjusTeade", pohjusTeade);
        command.Parameters.AddWithValue("@avaldaKodulehel", avaldaKodulehel);
        command.Parameters.AddWithValue("@asukohaUuendus", asukohaUuendusUtc);
        command.Parameters.AddWithValue("@reisiStaatus", reisiStaatus);
        command.Parameters.AddWithValue("@viimanePeatus", viimanePeatus);

        command.ExecuteNonQuery();
    }
}