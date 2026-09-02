-- @scenario Implicit conversion
-- @description Shows how an nvarchar parameter against a varchar indexed column can force conversion on the column.
-- @setup
IF EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Orders')
      AND name = N'IX_Orders_ExternalRef'
)
    DROP INDEX IX_Orders_ExternalRef ON dbo.Orders;

CREATE UNIQUE INDEX IX_Orders_ExternalRef
    ON dbo.Orders(ExternalRef);

-- @bad
DECLARE @ExternalRef nvarchar(20) = N'00000000000000123456';

SELECT Id, CustomerId, TotalAmount
FROM dbo.Orders
WHERE ExternalRef = @ExternalRef;

-- @good
DECLARE @ExternalRef varchar(20) = '00000000000000123456';

SELECT Id, CustomerId, TotalAmount
FROM dbo.Orders
WHERE ExternalRef = @ExternalRef;

-- @teardown
DROP INDEX IF EXISTS IX_Orders_ExternalRef ON dbo.Orders;
