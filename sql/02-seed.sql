-- Seed data from the practice plan (section 1.4). Run once, after 01-schema.sql.
-- Values are random, so your numbers will differ from anyone else's. That's fine.
-- It deliberately creates overbookings and 50 corrupted BookedPlayers counters (used in Mock D).

INSERT INTO Courses (Name, TimeZoneId, Country, Currency) VALUES
 ('Cedar Ridge GC','America/Chicago','US','USD'),
 ('Lakeside Links','America/New_York','US','USD'),
 ('Desert Palms','America/Phoenix','US','USD'),
 ('Hollow Moor','Europe/London','UK','GBP'),
 ('Kingsfield Park','Europe/London','UK','GBP');

-- 60 days x 67 slots (every 10 min) x 5 courses ~ 20k tee times
INSERT INTO TeeTimes (CourseId, StartUtc, Capacity, BookedPlayers, Price)
SELECT c.CourseId,
       DATEADD(MINUTE, s.i*10, DATEADD(HOUR, 12, DATEADD(DAY, d.i, CAST('2026-11-01' AS DATETIME2(0))))),
       4, 0,
       CAST(40 + ABS(CHECKSUM(NEWID())) % 60 AS DECIMAL(8,2))
FROM Courses c
CROSS JOIN (SELECT TOP (60) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1 AS i FROM sys.all_objects) d
CROSS JOIN (SELECT TOP (67) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1 AS i FROM sys.all_objects) s;

INSERT INTO Golfers (FirstName, LastName, Email, IsMember)
SELECT TOP (5000)
  CONCAT('First', n), CONCAT('Last', n % 900), CONCAT('golfer', n, '@example.com'),
  CASE WHEN n % 3 = 0 THEN 1 ELSE 0 END
FROM (SELECT ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS n
      FROM sys.all_objects a CROSS JOIN sys.all_objects b) x;

-- Random bookings: this WILL create overbookings on purpose (used in Mock D)
DECLARE @tt INT = (SELECT COUNT(*) FROM TeeTimes);
INSERT INTO Bookings (TeeTimeId, GolferId, Players, Status, CreatedUtc)
SELECT TOP (60000)
  1 + ABS(CHECKSUM(NEWID())) % @tt,
  1 + ABS(CHECKSUM(NEWID())) % 5000,
  1 + ABS(CHECKSUM(NEWID())) % 4,
  CASE ABS(CHECKSUM(NEWID())) % 10 WHEN 0 THEN 'Cancelled' WHEN 1 THEN 'Held' ELSE 'Confirmed' END,
  DATEADD(MINUTE, -(ABS(CHECKSUM(NEWID())) % 100000), SYSUTCDATETIME())
FROM sys.all_objects a CROSS JOIN sys.all_objects b;

-- Denormalized counter (capped at 255; some rows will be WRONG on purpose)
UPDATE t SET BookedPlayers = CASE WHEN x.P > 255 THEN 255 ELSE x.P END
FROM TeeTimes t
JOIN (SELECT TeeTimeId, SUM(Players) P FROM Bookings WHERE Status <> 'Cancelled' GROUP BY TeeTimeId) x
  ON x.TeeTimeId = t.TeeTimeId;
UPDATE TOP (50) TeeTimes SET BookedPlayers = 0 WHERE BookedPlayers > 0;  -- corrupt 50 rows

INSERT INTO Payments (BookingId, Amount, Currency, Status, CreatedUtc)
SELECT b.BookingId, t.Price * b.Players, c.Currency,
       CASE ABS(CHECKSUM(NEWID())) % 20 WHEN 0 THEN 'Failed' WHEN 1 THEN 'Refunded' ELSE 'Captured' END,
       DATEADD(SECOND, 30, b.CreatedUtc)
FROM Bookings b JOIN TeeTimes t ON t.TeeTimeId = b.TeeTimeId JOIN Courses c ON c.CourseId = t.CourseId
WHERE b.Status = 'Confirmed';
