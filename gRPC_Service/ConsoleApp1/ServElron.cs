using System.Globalization;
using System.Reflection.Metadata.Ecma335;

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
        decimal latitudeDecimal, longitudeDecimal;
        try
        {
            latitudeDecimal = decimal.Parse(latitude, CultureInfo.InvariantCulture);
            longitudeDecimal = decimal.Parse(longitude, CultureInfo.InvariantCulture);
        }
        catch (Exception ex)
        {
            Console.WriteLine("latitude " + latitude + " longitude " + longitude);
            Console.WriteLine(ex.Message);
            throw new Exception(ex.Message);
        }

        int rongiSuundInt = -1;
        try
        {
            rongiSuundInt = int.Parse(rongiSuund);
        }
        catch (Exception ex)
        {
            // Console.WriteLine("rongiSuund " + rongiSuund);
            // Console.WriteLine(ex);
            // throw;
            // return;
        }
        
        // Convert string to DateTimeOffset object
        DateTimeOffset? dateTimeOffset = null;
        try
        {
            dateTimeOffset = DateTimeOffset.ParseExact(asukohaUuendus, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        }
        catch (Exception) { }

        if (dateTimeOffset == null)
        {
            try
            {
                dateTimeOffset = DateTimeOffset.ParseExact(asukohaUuendus, "yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
            }
            catch (Exception) { }
        }

        if (dateTimeOffset == null)
        {
            return;
        }
        
        
        // DateTime asukohaUuendusDateTime = DateTime.Parse(asukohaUuendus);
        // DateTime asukohaUuendusUtc = asukohaUuendusDateTime.ToUniversalTime();

        
        using var connection = new NpgsqlConnection(_connectionString);
        connection.Open();

        using var command = new NpgsqlCommand("INSERT INTO serv_elron (reis, liin, reisi_algus_aeg, " +
                                              "reisi_lopp_aeg, kiirus, latitude, longitude, rongi_suund, erinevus_plaanist, lisateade, " +
                                              "pohjus_teade, avalda_kodulehel, asukoha_uuendus, reisi_staatus, viimane_peatus) " +
                                              "VALUES (@reis, @liin, @reisiAlgusAeg, @reisiLoppAeg, @kiirus, @latitude, @longitude, " +
                                              "@rongiSuund, @erinevusPlaanist, @lisateade, @pohjusTeade, @avaldaKodulehel, " +
                                              "@asukohaUuendus, @reisiStaatus, @viimanePeatus) " +
                                              "ON CONFLICT (asukoha_uuendus) DO UPDATE SET " +
                                              "reis = @reis, liin = @liin, reisi_algus_aeg = @reisiAlgusAeg, reisi_lopp_aeg = @reisiLoppAeg, " +
                                              "kiirus = @kiirus, latitude = @latitude, longitude = @longitude, rongi_suund = @rongiSuund, " +
                                              "erinevus_plaanist = @erinevusPlaanist, lisateade = @lisateade, pohjus_teade = @pohjusTeade, " +
                                              "avalda_kodulehel = @avaldaKodulehel, reisi_staatus = @reisiStaatus, viimane_peatus = @viimanePeatus", connection);



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
        command.Parameters.AddWithValue("@asukohaUuendus", dateTimeOffset?.UtcDateTime);
        command.Parameters.AddWithValue("@reisiStaatus", reisiStaatus);
        command.Parameters.AddWithValue("@viimanePeatus", viimanePeatus);

        command.ExecuteNonQuery();
    }
}