
### Business Requirements

1. **Validator Action**: In the "Valider Booking" page, add a "Temporiser" button
2. **Admin Input**: When postponing, admin must provide:
   - Reason for postponement
   - Estimated validation date
3. **Creator Response**: The user who created the booking can:
   - Accept the postponement
   - Refuse the postponement
4. **Workflow**:
   - If accepted → booking stays temporised until estimated date
   - If refused → booking returns to pending status

---



## SQL Server Implementation

### 1. Create BookingTemporisations Table

```sql
-- ============================================================================
-- TABLE: BookingTemporisations
-- PURPOSE: Track all booking postponement requests and responses
-- ============================================================================

CREATE TABLE BookingTemporisations (
    -- Primary Key
    TemporisationId INT PRIMARY KEY IDENTITY(1,1),

    -- Booking Reference
    BookingId INT NOT NULL,

    -- Temporisation Details (Admin Input)
    TemporisedByUserId INT NOT NULL,              -- Admin/Validator who postponed
    TemporisedAt DATETIME NOT NULL DEFAULT GETDATE(),
    ReasonTemporisation NVARCHAR(1000) NOT NULL,  -- Why it's postponed
    EstimatedValidationDate DATE NOT NULL,         -- When admin estimates validation

    -- Creator Response
    CreatorResponse NVARCHAR(50) NULL,            -- 'Pending', 'Accepted', 'Refused'
    CreatorRespondedAt DATETIME NULL,             -- When creator responded
    CreatorResponseNotes NVARCHAR(500) NULL,      -- Optional notes from creator

    -- Status Management
    IsActive BIT NOT NULL DEFAULT 1,              -- Is this the current active temporisation

    -- Audit Fields
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    UpdatedAt DATETIME NOT NULL DEFAULT GETDATE(),

    -- Foreign Key Constraints
    CONSTRAINT FK_BookingTemporisations_Booking
        FOREIGN KEY (BookingId) REFERENCES Bookings(BookingId) ON DELETE CASCADE,
    CONSTRAINT FK_BookingTemporisations_TemporisedBy
        FOREIGN KEY (TemporisedByUserId) REFERENCES Users(UserId),

    -- Check Constraints
    CONSTRAINT CK_BookingTemporisations_CreatorResponse
        CHECK (CreatorResponse IN ('Pending', 'Accepted', 'Refused'))
);
```





## Workflow Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                        BOOKING WORKFLOW                          │
└─────────────────────────────────────────────────────────────────┘

    [Booking Created]
         Status: Pending
              ↓
    ┌────────┴────────┐
    │                 │
[Validator Actions]   │
    │                 │
    ├─→ Validate      │ → Status: Validated ✓
    │                 │
    ├─→ Temporiser ───┤ → Status: Temporised
    │   (NEW)         │   Record created in BookingTemporisations
    │                 │   CreatorResponse: Pending
    └─→ Cancel        │ → Status: Cancelled

                      ↓
         [Creator Notification]
         "Your booking has been postponed"
                      ↓
              [Creator Response]
                      ↓
         ┌────────────┴────────────┐
         │                         │
    [Accept]                  [Refuse]
         │                         │
         ↓                         ↓
   Status: Temporised      Status: Pending
   CreatorResponse:        CreatorResponse: Refused
     Accepted              IsActive: 0
         │                 (Back to validator)
         ↓
   [Wait until
   EstimatedValidationDate]
         │
         ↓
   [Admin Validates]
         │
         ↓
   Status: Validated ✓
```

---
