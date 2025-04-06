-- Check if SuperAdmin exists
IF NOT EXISTS (SELECT 1 FROM Users WHERE Role = 'SuperAdmin')
BEGIN
    -- Insert SuperAdmin
    INSERT INTO Users (
        Email,
        Username,
        Password,
        Role,
        IsEmailVerified,
        IsActive,
        CreatedAt
    )
    VALUES (
        'superadmin@village.com',
        'superadmin',
        -- This is the hashed value of 'Admin@123' using SHA256
        'jZae727K08KaOmKSgOaGzww/XVqGr/PKEgIMkjrcbJI=',
        'SuperAdmin',
        1, -- IsEmailVerified = true
        1, -- IsActive = true
        GETUTCDATE()
    )
END 