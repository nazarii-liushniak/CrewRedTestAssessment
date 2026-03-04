-- Create Database
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'TaxiData')
BEGIN
    CREATE DATABASE [TaxiData];
END
GO

USE [TaxiData];
GO

-- Create Table
IF OBJECT_ID(N'[dbo].[TaxiTrips]', N'U') IS NULL
BEGIN
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
END
GO