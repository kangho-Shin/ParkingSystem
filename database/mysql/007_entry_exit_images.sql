ALTER TABLE parking_event
    ADD COLUMN groupnum INT NOT NULL DEFAULT 1 AFTER sitenum,
    ADD COLUMN imagepath VARCHAR(500) NULL AFTER eventat;

ALTER TABLE parking_session
    ADD COLUMN outflag CHAR(1) NULL AFTER status,
    ADD COLUMN indeviceid BIGINT NULL AFTER inlaneid,
    ADD COLUMN inimage VARCHAR(500) NULL AFTER indate,
    ADD COLUMN outdeviceid BIGINT NULL AFTER outlaneid,
    ADD COLUMN outimage VARCHAR(500) NULL AFTER outdate;

UPDATE parking_session
SET outflag = CASE status
    WHEN 'Entered' THEN 'I'
    WHEN 'Paid' THEN 'X'
    WHEN 'Exited' THEN 'O'
    ELSE 'I'
END;

ALTER TABLE parking_session
    DROP INDEX ix_parking_session_open_vehicle,
    MODIFY COLUMN outflag CHAR(1) NOT NULL DEFAULT 'I',
    DROP COLUMN status,
    ADD CONSTRAINT chk_parking_session_outflag CHECK (outflag IN ('I','X','O')),
    ADD INDEX ix_parking_session_open_vehicle (sitenum,carnum,outflag,indate);
