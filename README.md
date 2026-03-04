# CrewRed Test Assessment - Taxi Data ETL

This project implements a simple ETL process to import taxi trip data from a CSV file into a SQL Server database.

## Prerequisites

- .NET 8.0 SDK or later
- SQL Server running locally
- Input CSV file placed at `../../../sample-cab-data.csv` relative to the executable (or update the path in `Program.cs`)

## Setup & Execution

1.  **Database Setup**: The application automatically creates the `TaxiData` database and `TaxiTrips` table if they do not exist. Alternatively, you can run the `database_setup.sql` script manually.
2.  **Run the Application**:
    ```bash
    dotnet run
    ```
3.  **Output**:
    - The program will output the number of records processed, duplicates found, and rows inserted.
    - Duplicate records are written to `duplicates.csv`.

## Deliverables

### 1. Source Code
The source code is contained within this repository. Key files:
- `Program.cs`: Main entry point and ETL logic.
- `Mappers/TaxiTripMap.cs`: CSV parsing and data transformation logic.
- `Models/TaxiTrip.cs`: Data model representing a taxi trip.

### 2. SQL Scripts
See `database_setup.sql` for the database schema and index creation scripts.

### 3. Number of Rows
After running the program with the provided sample data:
- **Total Rows Inserted**: 29846
- **Duplicates Removed**: 154

### 4. Assumptions Made
- **Data Quality**: Records with NULL values in nullable fields (like `passenger_count` or `store_and_fwd_flag`) are considered valid and are inserted into the database rather than being discarded.
- **Timezone**: Input data is assumed to be in Eastern Standard Time (EST) and is converted to UTC during import.
- **Null Handling**: Only `passenger_count` and `store_and_fwd_flag` are nullable.
- **Environment**: The application assumes a local SQL Server instance is available at `Server=localhost;Database=TaxiData;Trusted_Connection=True;TrustServerCertificate=True;`.

## Handling Large Datasets

If the input file were 10GB or larger, the current in-memory approach would cause an `OutOfMemoryException`. To handle this scale, I would modify the application as follows:

1.  **Streaming Processing**: Instead of loading all records into a `List<TaxiTrip>`, I would use `CsvReader.GetRecords<TaxiTrip>()` which returns an `IEnumerable`. This allows processing records one by one or in small batches without loading the entire file into memory.
2.  **Batched Insertion**: I would implement a batching mechanism and perform `SqlBulkCopy` for each batch. This keeps memory usage constant regardless of file size.
3.  **Parallel Processing**: The ETL process could be parallelized using `Parallel.ForEach` or a producer-consumer pattern where one thread reads/parses the CSV and multiple threads handle database insertion.

## Schema Optimization for Queries

The database schema includes specific indexes to optimize the required queries:

- **Highest average tip by PULocationId**: `CREATE INDEX IX_PULocation_Tip ON TaxiTrips (PULocationID) INCLUDE (tip_amount);` allows the database to compute the average tip without scanning the entire table.
- **Top 100 longest distance**: `CREATE INDEX IX_TripDistance ON TaxiTrips (trip_distance DESC);` allows retrieving the top records instantly without sorting.
- **Top 100 longest duration**: A computed column `duration_seconds` is persisted and indexed (`IX_Duration`) to allow instant retrieval of the longest trips by time.
- **Search by PULocationId**: The `IX_PULocation_Tip` index covers this search efficiently.