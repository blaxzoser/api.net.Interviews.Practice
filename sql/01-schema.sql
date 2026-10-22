-- Tables only: no CREATE DATABASE / USE, so it runs against any database
-- (the dev database via scripts/db.ps1, and a throw-away database in the integration tests).
--
-- NOTE: deliberately NO indexes beyond PKs and NO check constraints. You add them in Mock D.

CREATE TABLE Courses (
  CourseId   INT IDENTITY PRIMARY KEY,
  Name       NVARCHAR(100) NOT NULL,
  TimeZoneId VARCHAR(50)   NOT NULL,
  Country    CHAR(2)       NOT NULL,
  Currency   CHAR(3)       NOT NULL
);

CREATE TABLE TeeTimes (
  TeeTimeId     INT IDENTITY PRIMARY KEY,
  CourseId      INT NOT NULL REFERENCES Courses(CourseId),
  StartUtc      DATETIME2(0) NOT NULL,
  Capacity      TINYINT NOT NULL DEFAULT 4,
  BookedPlayers TINYINT NOT NULL DEFAULT 0,
  Price         DECIMAL(8,2) NOT NULL,
  RowVersion    ROWVERSION
);

CREATE TABLE Golfers (
  GolferId  INT IDENTITY PRIMARY KEY,
  FirstName NVARCHAR(50) NOT NULL,
  LastName  NVARCHAR(50) NOT NULL,
  Email     VARCHAR(200) NOT NULL,
  IsMember  BIT NOT NULL
);

CREATE TABLE Bookings (
  BookingId      INT IDENTITY PRIMARY KEY,
  TeeTimeId      INT NOT NULL REFERENCES TeeTimes(TeeTimeId),
  GolferId       INT NOT NULL REFERENCES Golfers(GolferId),
  Players        TINYINT NOT NULL,
  Status         VARCHAR(10) NOT NULL,
  CreatedUtc     DATETIME2(0) NOT NULL,
  HoldExpiresUtc DATETIME2(0) NULL,
  IdempotencyKey VARCHAR(64) NULL
);

CREATE TABLE Payments (
  PaymentId  INT IDENTITY PRIMARY KEY,
  BookingId  INT NOT NULL REFERENCES Bookings(BookingId),
  Amount     DECIMAL(8,2) NOT NULL,
  Currency   CHAR(3) NOT NULL,
  Status     VARCHAR(10) NOT NULL,
  CreatedUtc DATETIME2(0) NOT NULL
);
