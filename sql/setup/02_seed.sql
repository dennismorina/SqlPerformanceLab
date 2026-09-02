SET NOCOUNT ON;

DECLARE @CustomerCount int = 50000;
DECLARE @OrderCount int = 250000;

;WITH Numbers AS
(
    SELECT TOP (@CustomerCount)
        ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS n
    FROM sys.all_objects a
    CROSS JOIN sys.all_objects b
)
INSERT INTO dbo.Customers (Id, CustomerCode, Name, Email)
SELECT
    n,
    CONCAT('CUS', RIGHT(CONCAT('00000000', n), 8)),
    CONCAT(N'Customer ', n),
    CONCAT('customer', n, '@example.test')
FROM Numbers;

;WITH Numbers AS
(
    SELECT TOP (@OrderCount)
        ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS n
    FROM sys.all_objects a
    CROSS JOIN sys.all_objects b
)
INSERT INTO dbo.Orders
(
    Id,
    CustomerId,
    CustomerCode,
    OrderDate,
    Status,
    ExternalRef,
    TotalAmount,
    Notes
)
SELECT
    n,
    ((n - 1) % @CustomerCount) + 1,
    CONCAT('CUS', RIGHT(CONCAT('00000000', ((n - 1) % @CustomerCount) + 1), 8)),
    DATEADD(
        minute,
        n % 1440,
        DATEADD(day, -(n % 1460), CONVERT(datetime2(0), CONVERT(date, SYSUTCDATETIME())))
    ),
    CASE n % 4
        WHEN 0 THEN 'New'
        WHEN 1 THEN 'Processing'
        WHEN 2 THEN 'Shipped'
        ELSE 'Completed'
    END,
    RIGHT(CONCAT('00000000000000000000', n), 20),
    CAST(((n % 50000) + 100) / 100.0 AS decimal(18,2)),
    CASE
        WHEN n % 10 = 0 THEN CONCAT(N'Priority order ', n)
        ELSE CONCAT(N'Standard order ', n)
    END
FROM Numbers;

UPDATE STATISTICS dbo.Customers WITH FULLSCAN;
UPDATE STATISTICS dbo.Orders WITH FULLSCAN;
GO
