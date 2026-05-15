-- Enforce the composite primary key for REP_ROLE_NEW.
-- EPF_NO identifies the person and USERTYPE distinguishes the role bucket.

ALTER TABLE REP_ROLE_NEW
  ADD CONSTRAINT PK_REP_ROLE_NEW PRIMARY KEY (EPF_NO, USERTYPE);
