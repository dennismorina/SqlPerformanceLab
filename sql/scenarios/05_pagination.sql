-- @scenario Offset vs keyset pagination
-- @description Compares deep OFFSET pagination with keyset pagination using the clustered primary key.
-- @setup
-- The clustered primary key on Orders(Id) is intentionally sufficient for both queries.

-- @bad
SELECT Id, CustomerId, OrderDate, TotalAmount
FROM dbo.Orders
ORDER BY Id
OFFSET 200000 ROWS
FETCH NEXT 50 ROWS ONLY;

-- @good
DECLARE @LastSeenId bigint = 200000;

SELECT TOP (50)
    Id,
    CustomerId,
    OrderDate,
    TotalAmount
FROM dbo.Orders
WHERE Id > @LastSeenId
ORDER BY Id;

-- @teardown
-- No scenario-specific index to remove.
