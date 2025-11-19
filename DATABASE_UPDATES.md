# Database Updates - User Table

## Date: 2025-11-17

### Changes to Users Table

The following columns have been added to the `Users` table:

- `FirstName` (NVARCHAR(100), NOT NULL)
- `LastName` (NVARCHAR(100), NOT NULL)
- `Email` (NVARCHAR(255), NOT NULL)

---

## SQL Scripts to Execute

### 1. Add New Columns to Users Table

```sql
-- Add FirstName column
ALTER TABLE Users
ADD FirstName NVARCHAR(100) NOT NULL DEFAULT '';

-- Add LastName column
ALTER TABLE Users
ADD LastName NVARCHAR(100) NOT NULL DEFAULT '';

-- Add Email column
ALTER TABLE Users
ADD Email NVARCHAR(255) NOT NULL DEFAULT '';
```

### 2. Update Existing Users (Optional - if you have existing data)

```sql
-- Update existing users with placeholder data
-- You should update this with real data for your existing users

UPDATE Users
SET
    FirstName = 'John',
    LastName = 'Doe',
    Email = Username + '@ltipn.com'
WHERE FirstName = '' OR FirstName IS NULL;
```

### 3. Add Email Unique Constraint (Recommended)

```sql
-- Add unique constraint on Email column
ALTER TABLE Users
ADD CONSTRAINT UQ_Users_Email UNIQUE (Email);
```

---

## Updated Table Structure

After applying the changes, the `Users` table will have:

```sql
Users (
    UserId INT PRIMARY KEY IDENTITY(1,1),
    Username NVARCHAR(100) NOT NULL,
    FirstName NVARCHAR(100) NOT NULL,      -- NEW
    LastName NVARCHAR(100) NOT NULL,       -- NEW
    Email NVARCHAR(255) NOT NULL,          -- NEW
    Password NVARCHAR(255) NOT NULL,
    Role NVARCHAR(50) NOT NULL,
    SocietyId INT NULL,
    TypeVoyage NVARCHAR(100) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    UpdatedAt DATETIME NOT NULL DEFAULT GETDATE()
)
```

---

## Sample Insert Statement

```sql
-- Example of inserting a new user with the new fields
INSERT INTO Users (Username, FirstName, LastName, Email, Password, Role, IsActive)
VALUES ('admin', 'Mohamed', 'Jaafar', 'admin@ltipn.com', 'admin123', 'Admin', 1);

INSERT INTO Users (Username, FirstName, LastName, Email, Password, Role, IsActive)
VALUES ('agent1', 'Ahmed', 'Benali', 'agent1@ltipn.com', 'agent123', 'Booking_Agent', 1);

INSERT INTO Users (Username, FirstName, LastName, Email, Password, Role, IsActive)
VALUES ('validator1', 'Fatima', 'El Amrani', 'validator1@ltipn.com', 'val123', 'Validator', 1);
```

---

## Verification Query

After applying the changes, verify the structure:

```sql
-- Check table structure
SELECT
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Users'
ORDER BY ORDINAL_POSITION;

-- View all users with new fields
SELECT
    UserId,
    Username,
    FirstName,
    LastName,
    Email,
    Role,
    IsActive
FROM Users;
```

---

## Impact on Application

### Model Changes:
- `User.cs` - Added FirstName, LastName, Email properties with validation
- Added `FullName` computed property: `FirstName + LastName`

### Controller Changes:
- `AccountController.cs` - Updated to include FirstName, LastName, Email in authentication claims

### View Changes:
- `_Layout.cshtml` - Updated to display full name instead of username
- User management views (Create, Edit, Details, Index) - Updated to include new fields

### Claims Added to Authentication:
- `ClaimTypes.GivenName` - FirstName
- `ClaimTypes.Surname` - LastName
- `ClaimTypes.Email` - Email
- `"FullName"` - Combined FirstName + LastName

---

## Notes

⚠️ **Important**:
1. Make sure to backup your database before running these scripts
2. Update existing user records with proper names and emails
3. Consider adding email validation and uniqueness constraints
4. Update any seed data scripts to include the new fields
