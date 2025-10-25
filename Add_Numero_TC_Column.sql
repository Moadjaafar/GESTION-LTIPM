-- Add Numero_TC column to Voyages table
-- This script adds the missing Numero_TC column that exists in the Voyage.cs model

USE [GESTION_LTIPN_DB]  -- Replace with your actual database name if different
GO

-- Check if column exists before adding
IF NOT EXISTS (SELECT * FROM sys.columns
               WHERE object_id = OBJECT_ID(N'[dbo].[Voyages]')
               AND name = 'Numero_TC')
BEGIN
    -- Add the column as NULLABLE first
    ALTER TABLE [dbo].[Voyages]
    ADD [Numero_TC] [nvarchar](50) NULL;

    PRINT 'Column Numero_TC added successfully.';

    -- Update existing records with a default value (empty string or a placeholder)
    UPDATE [dbo].[Voyages]
    SET [Numero_TC] = ''
    WHERE [Numero_TC] IS NULL;

    PRINT 'Existing records updated with default value.';

    -- Now make it NOT NULL
    ALTER TABLE [dbo].[Voyages]
    ALTER COLUMN [Numero_TC] [nvarchar](50) NOT NULL;

    PRINT 'Column Numero_TC set to NOT NULL.';
END
ELSE
BEGIN
    PRINT 'Column Numero_TC already exists.';
END
GO
