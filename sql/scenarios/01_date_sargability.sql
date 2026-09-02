-- @scenario Date range SARGability
-- @description Compares DATEDIFF on the indexed column with a seekable half-open date range.
-- @setup
IF EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Orders')
      AND name = N'IX_Orders_OrderDate'
)
    DROP INDEX IX_Orders_OrderDate ON dbo.Orders;

CREATE INDEX IX_Orders_OrderDate
    ON dbo.Orders(OrderDate);

-- @bad
DECLARE @TargetDate date =
    DATEADD(day, -30, CONVERT(date, SYSUTCDATETIME()));

SELECT COUNT_BIG(*)
FROM dbo.Orders
WHERE DATEDIFF(day, OrderDate, @TargetDate) = 0;

-- @good
DECLARE @TargetDate date =
    DATEADD(day, -30, CONVERT(date, SYSUTCDATETIME()));

SELECT COUNT_BIG(*)
FROM dbo.Orders
WHERE OrderDate >= @TargetDate
  AND OrderDate < DATEADD(day, 1, @TargetDate);

-- @teardown
DROP INDEX IF EXISTS IX_Orders_OrderDate ON dbo.Orders;
