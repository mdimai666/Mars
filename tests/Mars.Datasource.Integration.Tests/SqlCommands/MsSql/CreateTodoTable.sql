CREATE TABLE todo (
    id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(), -- Guid Id (UNIQUEIDENTIFIER in MSSQL)
    title NVARCHAR(255) NOT NULL,                   -- String Title
    content NVARCHAR(MAX),                         -- String Content
    completed BIT NOT NULL DEFAULT 0               -- Bool Completed (defaults to FALSE)
);
