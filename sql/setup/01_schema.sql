SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.Orders', N'U') IS NOT NULL
    DROP TABLE dbo.Orders;

IF OBJECT_ID(N'dbo.Customers', N'U') IS NOT NULL
    DROP TABLE dbo.Customers;

CREATE TABLE dbo.Customers
(
    Id            int                  NOT NULL,
    CustomerCode  varchar(20)       NOT NULL,
    Name          nvarchar(150)     NOT NULL,
    Email         varchar(200)      NOT NULL,

    CONSTRAINT PK_Customers PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_Customers_CustomerCode UNIQUE (CustomerCode)
);

CREATE TABLE dbo.Orders
(
    Id            bigint               NOT NULL,
    CustomerId    int                  NOT NULL,
    CustomerCode  varchar(20)          NOT NULL,
    OrderDate     datetime2(0)         NOT NULL,
    Status        varchar(20)          NOT NULL,
    ExternalRef   varchar(20)          NOT NULL,
    TotalAmount   decimal(18,2)        NOT NULL,
    Notes         nvarchar(200)        NULL,

    CONSTRAINT PK_Orders PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_Orders_Customers
        FOREIGN KEY (CustomerId) REFERENCES dbo.Customers(Id)
);
GO
