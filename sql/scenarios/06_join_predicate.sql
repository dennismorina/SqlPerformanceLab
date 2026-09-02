-- @scenario SARGable join predicate
-- @description Compares a function applied to the join key with a direct indexed equality join.
-- @setup
IF EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Orders')
      AND name = N'IX_Orders_CustomerCode'
)
    DROP INDEX IX_Orders_CustomerCode ON dbo.Orders;

CREATE INDEX IX_Orders_CustomerCode
    ON dbo.Orders(CustomerCode)
    INCLUDE (OrderDate, TotalAmount);

-- @bad
DECLARE @CustomerCode varchar(20) = 'CUS00012345';

SELECT o.Id, o.OrderDate, o.TotalAmount
FROM dbo.Customers c
INNER JOIN dbo.Orders o
    ON UPPER(o.CustomerCode) = c.CustomerCode
WHERE c.CustomerCode = @CustomerCode;

-- @good
DECLARE @CustomerCode varchar(20) = 'CUS00012345';

SELECT o.Id, o.OrderDate, o.TotalAmount
FROM dbo.Customers c
INNER JOIN dbo.Orders o
    ON o.CustomerCode = c.CustomerCode
WHERE c.CustomerCode = @CustomerCode;

-- @teardown
DROP INDEX IF EXISTS IX_Orders_CustomerCode ON dbo.Orders;
