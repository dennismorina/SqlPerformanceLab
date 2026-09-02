-- @scenario Function on indexed column
-- @description Compares LOWER(column) filtering with a direct equality predicate on normalized data.
-- @setup
IF EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Customers')
      AND name = N'IX_Customers_Email'
)
    DROP INDEX IX_Customers_Email ON dbo.Customers;

CREATE UNIQUE INDEX IX_Customers_Email
    ON dbo.Customers(Email);

-- @bad
DECLARE @Email varchar(200) = 'customer12345@example.test';

SELECT Id, CustomerCode, Name
FROM dbo.Customers
WHERE LOWER(Email) = LOWER(@Email);

-- @good
DECLARE @Email varchar(200) = 'customer12345@example.test';

SELECT Id, CustomerCode, Name
FROM dbo.Customers
WHERE Email = @Email;

-- @teardown
DROP INDEX IF EXISTS IX_Customers_Email ON dbo.Customers;
