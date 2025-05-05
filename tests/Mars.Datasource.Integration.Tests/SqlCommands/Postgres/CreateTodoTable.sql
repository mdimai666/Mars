CREATE TABLE todo (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(), -- Guid Id (UUID in PostgreSQL)
    title VARCHAR(255) NOT NULL,                  -- String Title
    content TEXT,                                  -- String Content
    completed BOOLEAN NOT NULL DEFAULT FALSE       -- Bool Completed (defaults to FALSE)
);
