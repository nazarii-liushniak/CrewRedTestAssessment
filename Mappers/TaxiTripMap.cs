using CrewRed_Test_Assessment.Models;
using CsvHelper.Configuration;

namespace CrewRed_Test_Assessment.Mappers;

public sealed class TaxiTripMap: ClassMap<TaxiTrip>
{
    public TaxiTripMap()
    {
        Map(m => m.PassengerCount).Name("passenger_count").Convert(args => {
            var value = args.Row.GetField("passenger_count");
            return int.TryParse(value, out var result) ? result : null;
        });
        Map(m => m.TripDistance).Name("trip_distance");
        Map(m => m.PuLocationId).Name("PULocationID");
        Map(m => m.DoLocationId).Name("DOLocationID");
        Map(m => m.FareAmount).Name("fare_amount");
        Map(m => m.TipAmount).Name("tip_amount");

        // Trim whitespaces and convert Y/N to Yes/No
        Map(m => m.StoreAndFwdFlag).Name("store_and_fwd_flag").Convert(args =>
        {
            var value = args.Row.GetField("store_and_fwd_flag")?.Trim();

            if (string.IsNullOrEmpty(value))
                return null;
            
            return value == "Y" ? "Yes" : "No";
        });

        // Convert EST to UTC
        var estZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
        
        Map(m => m.TpepPickupDatetime).Name("tpep_pickup_datetime").Convert(args =>
        {
            var value = args.Row.GetField("tpep_pickup_datetime");
            
            return DateTime.TryParse(value, out var dt)
                ? TimeZoneInfo.ConvertTimeToUtc(dt, estZone)
                : default;
        });

        Map(m => m.TpepDropoffDatetime).Name("tpep_dropoff_datetime").Convert(args =>
        {
            var value = args.Row.GetField("tpep_dropoff_datetime");
            
            return DateTime.TryParse(value, out var dt)
                ? TimeZoneInfo.ConvertTimeToUtc(dt, estZone)
                : default;
        });
    }
}