CREATE TABLE IF NOT EXISTS parking_event
(
    eventid BINARY(16) NOT NULL,
    sitenum BIGINT NOT NULL,
    groupnum INT NOT NULL DEFAULT 1,
    laneid BIGINT NOT NULL,
    deviceid BIGINT NOT NULL,
    eventtype VARCHAR(20) NOT NULL,
    carnum VARCHAR(20) NOT NULL,
    eventat DATETIME NOT NULL,
    imagepath VARCHAR(500) NULL,
    resultjson JSON NULL,
    createdat DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (eventid),
    INDEX ix_parking_event_site_time (sitenum, eventat)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS parking_session
(
    xindex BIGINT NOT NULL AUTO_INCREMENT,
    sitenum BIGINT NOT NULL,
    ineventid BINARY(16) NOT NULL,
    carnum VARCHAR(20) NOT NULL,
    groupnum INT NOT NULL DEFAULT 1,
    cartype INT NOT NULL DEFAULT 1,
    inlaneid BIGINT NOT NULL,
    indeviceid BIGINT NULL,
    indate DATETIME NOT NULL,
    inimage VARCHAR(500) NULL,
    outeventid BINARY(16) NULL,
    outlaneid BIGINT NULL,
    outdeviceid BIGINT NULL,
    outdate DATETIME NULL,
    outimage VARCHAR(500) NULL,
    outflag CHAR(1) NOT NULL DEFAULT 'I',
    PRIMARY KEY (xindex),
    UNIQUE KEY ux_parking_session_entry_event (ineventid),
    INDEX ix_parking_session_open_vehicle (sitenum, carnum, outflag, indate),
    CONSTRAINT chk_parking_session_outflag CHECK (outflag IN ('I','X','O'))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
