using CsvHelper.Configuration.Attributes;

namespace CrewRed_Test_Assessment.Models;

public class TaxiTrip
{
    [Name("tpep_pickup_datetime")]
    [Format("MM/dd/yyyy hh:mm:ss tt")]
    public DateTime TpepPickupDatetime { get; set; }

    [Name("tpep_dropoff_datetime")]
    [Format("MM/dd/yyyy hh:mm:ss tt")]
    public DateTime TpepDropoffDatetime { get; set; }

    [Name("passenger_count")]
    public int? PassengerCount { get; set; }

    [Name("trip_distance")]
    public double TripDistance { get; set; }

    [Name("store_and_fwd_flag")]
    public string? StoreAndFwdFlag { get; set; }

    [Name("PULocationID")]
    public int PuLocationId { get; set; }

    [Name("DOLocationID")]
    public int DoLocationId { get; set; }

    [Name("fare_amount")]
    public decimal FareAmount { get; set; }

    [Name("tip_amount")]
    public decimal TipAmount { get; set; }
}