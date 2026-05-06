-- Normalized report parameter mapping table
-- Run this script in Oracle before using /api/populate from V2 controller.

CREATE TABLE report_parameters (
    repid      VARCHAR2(50)   NOT NULL,
    paraname   VARCHAR2(100)  NOT NULL,
    value      VARCHAR2(100)  DEFAULT '0' NOT NULL,
    created_at DATE           DEFAULT SYSDATE NOT NULL
);

-- Optional FK if REP_REPORTS_NEW.REPID is unique and cleanly constrained.
-- ALTER TABLE report_parameters
--   ADD CONSTRAINT fk_report_parameters_report
--   FOREIGN KEY (repid) REFERENCES rep_reports_new (repid);

-- Optional FK if REP_REPORT_PARAMS_NEW.PARANAME is unique and cleanly constrained.
-- ALTER TABLE report_parameters
--   ADD CONSTRAINT fk_report_parameters_parameter
--   FOREIGN KEY (paraname) REFERENCES rep_report_params_new (paraname);

-- Concurrency and deduplication guard (case-insensitive unique key)
CREATE UNIQUE INDEX uq_report_parameters_rep_para_ci
    ON report_parameters (UPPER(TRIM(repid)), UPPER(TRIM(paraname)));

-- Supporting lookup indexes
CREATE INDEX ix_report_parameters_repid
    ON report_parameters (UPPER(TRIM(repid)));

CREATE INDEX ix_report_parameters_paraname
    ON report_parameters (UPPER(TRIM(paraname)));

-- Optimized, idempotent populate statement
-- This is the same pattern used by the V2 repository.
-- INSERT INTO report_parameters (repid, paraname, value)
-- SELECT r.repid, p.paraname, '0'
-- FROM rep_reports_new r
-- CROSS JOIN rep_report_params_new p
-- WHERE TRIM(p.paraname) IS NOT NULL
--   AND NOT EXISTS (
--       SELECT 1
--       FROM report_parameters rp
--       WHERE UPPER(TRIM(rp.repid)) = UPPER(TRIM(r.repid))
--         AND UPPER(TRIM(rp.paraname)) = UPPER(TRIM(p.paraname))
--   );
