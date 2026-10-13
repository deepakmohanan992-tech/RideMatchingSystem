-- Script to create RideMatchingDb and tables for Drivers, Riders, and Rides
-- Run using sqlcmd or in SSMS connected to localhost\SQLEXPRESS

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'RideMatchingDb')
BEGIN
	CREATE DATABASE [RideMatchingDb];
END
GO

USE [RideMatchingDb];
GO

IF OBJECT_ID('dbo.Drivers', 'U') IS NULL
BEGIN
	CREATE TABLE dbo.Drivers
	(
		Id INT IDENTITY(1,1) PRIMARY KEY,
		Name NVARCHAR(200) NULL,
		Latitude FLOAT NULL,
		Longitude FLOAT NULL,
		IsAvailable BIT NOT NULL DEFAULT 0,
		LastSeenUtc DATETIME2 NULL,
		RowVersion ROWVERSION
	);
END
GO

IF OBJECT_ID('dbo.Riders', 'U') IS NULL
BEGIN
	CREATE TABLE dbo.Riders
	(
		Id INT IDENTITY(1,1) PRIMARY KEY,
		Name NVARCHAR(200) NULL
	);
END
GO

IF OBJECT_ID('dbo.Rides', 'U') IS NULL
BEGIN
	CREATE TABLE dbo.Rides
	(
		Id INT IDENTITY(1,1) PRIMARY KEY,
		RiderId INT NOT NULL,
		DriverId INT NULL,
		PickupLatitude FLOAT NOT NULL,
		PickupLongitude FLOAT NOT NULL,
		DropoffLatitude FLOAT NULL,
		DropoffLongitude FLOAT NULL,
		Status NVARCHAR(50) NOT NULL,
		RequestedAtUtc DATETIME2 NOT NULL,
		AssignedAtUtc DATETIME2 NULL,
		CompletedAtUtc DATETIME2 NULL,
		CONSTRAINT FK_Rides_Riders FOREIGN KEY (RiderId) REFERENCES dbo.Riders(Id),
		CONSTRAINT FK_Rides_Drivers FOREIGN KEY (DriverId) REFERENCES dbo.Drivers(Id)
	);
END
GO
