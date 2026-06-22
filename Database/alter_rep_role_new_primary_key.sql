-- 1. Drop the existing primary key constraint on REP_ROLE_NEW (which is currently on ROLEID)
-- Using CASCADE to drop any referencing foreign keys if they exist.
ALTER TABLE REP_ROLE_NEW DROP PRIMARY KEY CASCADE;

-- 2. Ensure both EPF_NO and USERTYPE are NOT NULL
ALTER TABLE REP_ROLE_NEW MODIFY (EPF_NO NOT NULL, USERTYPE NOT NULL);

-- 3. Add the new composite primary key constraint on (EPF_NO, USERTYPE)
ALTER TABLE REP_ROLE_NEW ADD CONSTRAINT PK_REP_ROLE_NEW PRIMARY KEY (EPF_NO, USERTYPE);
