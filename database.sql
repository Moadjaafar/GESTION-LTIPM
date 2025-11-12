-- GESTION-LTIPN Database Script
-- Created: 2025-11-12
CREATE DATABASE LTIPM_db;
GO

-- Use the database
USE LTIPM_db;
GO

-- Societies Table
CREATE TABLE Societies (
    SocietyId INT PRIMARY KEY IDENTITY(1,1),
    SocietyName NVARCHAR(200) NOT NULL UNIQUE,
    Address NVARCHAR(500) NULL,
    City NVARCHAR(100) NULL,
    Phone NVARCHAR(50) NULL,
    Email NVARCHAR(100) NULL,
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME DEFAULT GETDATE(),
    UpdatedAt DATETIME DEFAULT GETDATE()
);
-- Societies_Transp Table (Transport Companies)
CREATE TABLE Societies_Transp (
    SocietyTranspId INT PRIMARY KEY IDENTITY(1,1),
    SocietyTranspName NVARCHAR(200) NOT NULL UNIQUE,
    Address NVARCHAR(500) NULL,
    City NVARCHAR(100) NULL,
    Phone NVARCHAR(50) NULL,
    Email NVARCHAR(100) NULL,
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME DEFAULT GETDATE(),
    UpdatedAt DATETIME DEFAULT GETDATE()
);

-- Camions Table
CREATE TABLE Camions (
    CamionId INT PRIMARY KEY IDENTITY(1,1),
    CamionMatricule NVARCHAR(50) NOT NULL UNIQUE,
    DriverName NVARCHAR(100) NULL,
    DriverPhone NVARCHAR(50) NULL,
    CamionType NVARCHAR(50) NULL, -- 'Refrigerated', 'Standard', etc.
    SocietyTranspId INT NOT NULL,
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME DEFAULT GETDATE(),
    UpdatedAt DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (SocietyTranspId) REFERENCES Societies_Transp(SocietyTranspId)
);

-- Insert sample camions
INSERT INTO Camions (CamionMatricule, DriverName, DriverPhone, CamionType ,SocietyTranspId) VALUES
( 'A-12345-B', 'Mohammed Alami', '+212-xxx-xxxx', 'EXTERNE', 1),
('A-67890-B', 'Ahmed Bennani', '+212-xxx-xxxx', 'INTERNE',1),
('A-11111-B', 'Youssef Idrissi', '+212-xxx-xxxx', 'INTERNE',2);

-- Users Table
CREATE TABLE Users (
    UserId INT PRIMARY KEY IDENTITY(1,1),
    Username NVARCHAR(100) NOT NULL UNIQUE,
    Password NVARCHAR(255) NOT NULL,
    Role NVARCHAR(50) NOT NULL, -- e.g., 'Admin', 'Validator', 'Booking_Agent'
    SocietyId INT NULL,
    TypeVoyage NVARCHAR(100) NULL,
    CreatedAt DATETIME DEFAULT GETDATE(),
    UpdatedAt DATETIME DEFAULT GETDATE(),
    IsActive BIT DEFAULT 1,
    FOREIGN KEY (SocietyId) REFERENCES Societies(SocietyId)
);

-- Bookings Table
CREATE TABLE Bookings (
    BookingId INT PRIMARY KEY IDENTITY(1,1),
    BookingReference NVARCHAR(50) UNIQUE NOT NULL,
    SocietyId INT NOT NULL,
    TypeVoyage NVARCHAR(100) NOT NULL,
    Nbr_LTC INT NOT NULL, -- Number of voyages (LTC = Lot de Transport Camion)
    CreatedByUserId INT NOT NULL,
    ValidatedByUserId INT NULL,
    BookingStatus NVARCHAR(50) NOT NULL DEFAULT 'Pending', -- 'Pending', 'Validated', 'Completed', 'Cancelled'
    CreatedAt DATETIME DEFAULT GETDATE(),
    ValidatedAt DATETIME NULL,
    Notes NVARCHAR(MAX),
    FOREIGN KEY (SocietyId) REFERENCES Societies(SocietyId),
    FOREIGN KEY (CreatedByUserId) REFERENCES Users(UserId),
    FOREIGN KEY (ValidatedByUserId) REFERENCES Users(UserId)
);


select * from Bookings

-- =====================================================
-- NEW: Voyages_New Table with Two Camion Support
-- =====================================================
CREATE TABLE dbo.Voyages (
    VoyageId INT IDENTITY(1,1) PRIMARY KEY,
    BookingId INT NOT NULL,
    VoyageNumber INT NOT NULL,

    SocietyPrincipaleId INT NOT NULL,
    SocietySecondaireId INT NULL,

    CamionFirstDepart INT NULL,
    CamionSecondDepart INT NULL,
    CamionMatricule_FirstDepart_Externe NVARCHAR(50) NULL,
    CamionMatricule_SecondDepart_Externe NVARCHAR(50) NULL,

    DepartureCity NVARCHAR(50) NULL,
    DepartureDate DATE NULL,
    DepartureTime TIME(7) NULL,
    DepartureType NVARCHAR(50) NULL,

    ReceptionDate DATE NULL,
    ReceptionTime TIME(7) NULL,

    ReturnDepartureDate DATE NULL,
    ReturnDepartureTime TIME(7) NULL,
    ReturnArrivalCity NVARCHAR(50) NULL,
    ReturnArrivalDate DATE NULL,
    ReturnArrivalTime TIME(7) NULL,

    IsValidated BIT NOT NULL DEFAULT (0),
    ValidatedByUserId INT NULL,
    ValidatedAt DATETIME NULL,

    PricePrincipale DECIMAL(18,2) NULL,
    PriceSecondaire DECIMAL(18,2) NULL,
    Currency NVARCHAR(10) NOT NULL DEFAULT ('MAD'),
    VoyageStatus NVARCHAR(50) NOT NULL DEFAULT ('Planned'),

    CreatedAt DATETIME NOT NULL DEFAULT (GETDATE()),
    UpdatedAt DATETIME NOT NULL DEFAULT (GETDATE()),

    Type_Emballage NVARCHAR(200) NULL,
    numero_TC NVARCHAR(50) NULL,

    CONSTRAINT FK_Voyages_Bookings FOREIGN KEY (BookingId) REFERENCES dbo.Bookings(BookingId),
    CONSTRAINT FK_Voyages_CamionFirstDepart FOREIGN KEY (CamionFirstDepart) REFERENCES dbo.Camions(CamionId),
    CONSTRAINT FK_Voyages_CamionSecondDepart FOREIGN KEY (CamionSecondDepart) REFERENCES dbo.Camions(CamionId),
    CONSTRAINT FK_Voyages_SocietyPrincipale FOREIGN KEY (SocietyPrincipaleId) REFERENCES dbo.Societies(SocietyId),
    CONSTRAINT FK_Voyages_SocietySecondaire FOREIGN KEY (SocietySecondaireId) REFERENCES dbo.Societies(SocietyId),
    CONSTRAINT FK_Voyages_ValidatedByUser FOREIGN KEY (ValidatedByUserId) REFERENCES dbo.Users(UserId)
);




UPDATE Voyages
  SET DepartureType = 'Empty'
  WHERE DepartureType IS NULL;



select * from Voyages

-- Sample data insert for testing
-- Insert sample societies
INSERT INTO Societies (SocietyName, Address, City, Phone, Email) VALUES
('ERG CONSERVE', 'Dakhla', 'Dakhla', '+212-xxx-xxxx', 'contact@principale.ma'),
('ERG PACKAGING', 'Dakhla', 'Dakhla', '+212-xxx-xxxx', 'dakhla@principale.ma'),
('ERG DELICE', 'Dakhla', 'Dakhla', '+212-xxx-xxxx', 'casa@principale.ma');


INSERT INTO Societies_Transp (SocietyTranspName, Address, City, Phone, Email) VALUES
('LTIPM', 'Dakhla', 'Dakhla', '+212-xxx-xxxx', 'contact@principale.ma'),
('HABSA', 'Dakhla', 'Dakhla', '+212-xxx-xxxx', 'dakhla@principale.ma');


DELETE FROM Voyages;

-- Insert sample users


