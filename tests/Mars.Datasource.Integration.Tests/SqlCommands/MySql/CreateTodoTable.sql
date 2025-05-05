CREATE TABLE todo (
    id CHAR(36) PRIMARY KEY DEFAULT (UUID()), -- Guid Id (UUID in MySQL)
    title VARCHAR(255) NOT NULL,              -- String Title
    content TEXT,                             -- String Content
    completed BOOLEAN NOT NULL DEFAULT FALSE  -- Bool Completed (defaults to FALSE)
);
