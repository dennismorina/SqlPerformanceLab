-- @scenario Covering index
-- @description Contrasts a forced clustered scan with the seekable covering index designed for the query shape.
-- @setup
IF EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Orders')
      AND name = N'IX_Orders_Status_OrderDate_Covering'
)
    DROP INDEX IX_Orders_Status_OrderDate_Covering ON dbo.Orders;

CREATE INDEX IX_Orders_Status_OrderDate_Covering
    ON dbo.Orders(Status, OrderDate)
    INCLUDE (CustomerId, TotalAmount);

-- @bad
DECLARE @FromDate datetime2(0) =
    DATEADD(day, -30, CONVERT(datetime2(0), CONVERT(date, SYSUTCDATETIME())));

SELECT CustomerId, OrderDate, TotalAmount
FROM dbo.Orders WITH (INDEX(PK_Orders))
WHERE Status = 'Processing'
  AND OrderDate >= @FromDate;

-- @good
DECLARE @FromDate datetime2(0) =
    DATEADD(day, -30, CONVERT(datetime2(0), CONVERT(date, SYSUTCDATETIME())));

SELECT CustomerId, OrderDate, TotalAmount
FROM dbo.Orders
WHERE Status = 'Processing'
  AND OrderDate >= @FromDate;

-- @teardown
DROP INDEX IF EXISTS IX_Orders_Status_OrderDate_Covering ON dbo.Orders;
