using System.Data;
using System.Globalization;
using CrewRed_Test_Assessment.Mappers;
using CrewRed_Test_Assessment.Models;
using CsvHelper;
using Microsoft.Data.SqlClient;

namespace CrewRed_Test_Assessment;

public class Program
{
    private const string ConnectionString = "Server=localhost;Database=TaxiData;Trusted_Connection=True;TrustServerCertificate=True;";
    private const string InputPath = "../../../sample-cab-data.csv";
    private const string DuplicatePath = "../../../duplicates.csv";
    
    private static readonly List<TaxiTrip> CleanRecords = [];
    private static readonly List<TaxiTrip> DuplicateRecords = [];
    private static readonly HashSet<(DateTime, DateTime, int?)> SeenKeys = [];
    
    public static void Main()
    {
        try 
        {
            EnsureDatabaseSetup(ConnectionString);

            Console.WriteLine("Reading CSV file...");
            var records = ExtractFromCsv(InputPath);
            
            Console.WriteLine($"Total records read: {records.Count}");
            
            Transform(records);
            
            Console.WriteLine($"Clean records to insert: {CleanRecords.Count}");
            Console.WriteLine($"Duplicate records found: {DuplicateRecords.Count}");
            
            Load();
            
            Console.WriteLine("Data import completed successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Critical Error: {ex.Message}");
        }
    }

    private static List<TaxiTrip> ExtractFromCsv(string filePath)
    {
        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        csv.Context.RegisterClassMap<TaxiTripMap>();

        return csv.GetRecords<TaxiTrip>().ToList();
    }

    private static void Transform(List<TaxiTrip> records)
    {
        foreach (var record in records)
        {
            var key = (record.TpepPickupDatetime, record.TpepDropoffDatetime, record.PassengerCount);
                
            if (!SeenKeys.Add(key))
                DuplicateRecords.Add(record);
            else
                CleanRecords.Add(record);
        }
    }

    private static void Load()
    {
        BulkCopyToDatabase();
        WriteDuplicates();
    }

    private static void BulkCopyToDatabase()
    {
        if (CleanRecords.Count == 0) return;
        
        using var bulkCopy = new SqlBulkCopy(ConnectionString);
        bulkCopy.DestinationTableName = "TaxiTrips";
        bulkCopy.BatchSize = 5000;

        // Map Class Properties to SQL Columns
        bulkCopy.ColumnMappings.Add(nameof(TaxiTrip.TpepPickupDatetime), "tpep_pickup_datetime");
        bulkCopy.ColumnMappings.Add(nameof(TaxiTrip.TpepDropoffDatetime), "tpep_dropoff_datetime");
        bulkCopy.ColumnMappings.Add(nameof(TaxiTrip.PassengerCount), "passenger_count");
        bulkCopy.ColumnMappings.Add(nameof(TaxiTrip.TripDistance), "trip_distance");
        bulkCopy.ColumnMappings.Add(nameof(TaxiTrip.StoreAndFwdFlag), "store_and_fwd_flag");
        bulkCopy.ColumnMappings.Add(nameof(TaxiTrip.PuLocationId), "PULocationID");
        bulkCopy.ColumnMappings.Add(nameof(TaxiTrip.DoLocationId), "DOLocationID");
        bulkCopy.ColumnMappings.Add(nameof(TaxiTrip.FareAmount), "fare_amount");
        bulkCopy.ColumnMappings.Add(nameof(TaxiTrip.TipAmount), "tip_amount");

        var table = ConvertListToDataTable(CleanRecords);
        
        using var connection = new SqlConnection(ConnectionString);
        connection.Open();
        bulkCopy.WriteToServer(table);
    }

    private static void WriteDuplicates()
    {
        using var writer = new StreamWriter(DuplicatePath);
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        csv.WriteRecords(DuplicateRecords);
    }

    private static DataTable ConvertListToDataTable(List<TaxiTrip> data)
    {
        var table = new DataTable();
        
        table.Columns.Add(nameof(TaxiTrip.TpepPickupDatetime), typeof(DateTime));
        table.Columns.Add(nameof(TaxiTrip.TpepDropoffDatetime), typeof(DateTime));
        table.Columns.Add(nameof(TaxiTrip.PassengerCount), typeof(int)).AllowDBNull = true;
        table.Columns.Add(nameof(TaxiTrip.TripDistance), typeof(double));
        table.Columns.Add(nameof(TaxiTrip.StoreAndFwdFlag), typeof(string)).AllowDBNull = true;
        table.Columns.Add(nameof(TaxiTrip.PuLocationId), typeof(int));
        table.Columns.Add(nameof(TaxiTrip.DoLocationId), typeof(int));
        table.Columns.Add(nameof(TaxiTrip.FareAmount), typeof(decimal));
        table.Columns.Add(nameof(TaxiTrip.TipAmount), typeof(decimal));

        foreach (var item in data)
        {
            table.Rows.Add(
                item.TpepPickupDatetime,
                item.TpepDropoffDatetime,
                item.PassengerCount.HasValue ? item.PassengerCount.Value : DBNull.Value,
                item.TripDistance,
                item.StoreAndFwdFlag ?? (object)DBNull.Value,
                item.PuLocationId,
                item.DoLocationId,
                item.FareAmount,
                item.TipAmount
            );
        }
        
        return table;
    }
    
    private static void EnsureDatabaseSetup(string connectionString)
    {
        var builder = new SqlConnectionStringBuilder(connectionString);
        var targetDatabase = builder.InitialCatalog;
        
        builder.InitialCatalog = "master";
        using (var connection = new SqlConnection(builder.ConnectionString))
        {
            connection.Open();
            
            Console.WriteLine($"Ensuring database '{targetDatabase}' exists...");
            var createDbSql = $@"
                IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = '{targetDatabase}')
                BEGIN
                    CREATE DATABASE [{targetDatabase}];
                END";
            
            using var command = new SqlCommand(createDbSql, connection);
            command.ExecuteNonQuery();
        }

        using (var connection = new SqlConnection(connectionString))
        {
            connection.Open();

            Console.WriteLine("Ensuring table and indexes exist...");
            const string createTableSql = """
                DROP TABLE IF EXISTS TaxiTrips;

                CREATE TABLE TaxiTrips (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    tpep_pickup_datetime DATETIME2 NOT NULL,
                    tpep_dropoff_datetime DATETIME2 NOT NULL,
                    passenger_count INT NULL,
                    trip_distance FLOAT NOT NULL,
                    store_and_fwd_flag VARCHAR(3) NULL,
                    PULocationID INT NOT NULL,
                    DOLocationID INT NOT NULL,
                    fare_amount DECIMAL(10, 2) NOT NULL,
                    tip_amount DECIMAL(10, 2) NOT NULL,
                    -- Persisted computed column for duration queries
                    duration_seconds AS DATEDIFF_BIG(SECOND, tpep_pickup_datetime, tpep_dropoff_datetime) PERSISTED
                );

                -- Optimization Indexes
                CREATE INDEX IX_PULocation_Tip ON TaxiTrips (PULocationID) INCLUDE (tip_amount);
                CREATE INDEX IX_TripDistance ON TaxiTrips (trip_distance DESC);
                CREATE INDEX IX_Duration ON TaxiTrips (duration_seconds DESC);
                """;

            using var command = new SqlCommand(createTableSql, connection);
            command.ExecuteNonQuery();
        }
    }
}