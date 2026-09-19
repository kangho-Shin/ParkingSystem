CREATE TABLE parking_event
(
    eventid BINARY(16) NOT NULL,
    sitenum BIGINT NOT NULL,
    laneid BIGINT NOT NULL,
    deviceid BIGINT NOT NULL,
    eventtype VARCHAR(20) NOT NULL,
    carnum VARCHAR(20) NOT NULL,
    eventat DATETIME(6) NOT NULL,
    resultjson JSON NULL,
    createdat DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    PRIMARY KEY (eventid),
    INDEX ix_parking_event_site_time (sitenum, eventat)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE parking_session
(
    xindex BIGINT NOT NULL AUTO_INCREMENT,
    sitenum BIGINT NOT NULL,
    ineventid BINARY(16) NOT NULL,
    carnum VARCHAR(20) NOT NULL,
    groupnum INT NOT NULL DEFAULT 1,
    cartype INT NOT NULL DEFAULT 1,
    inlaneid BIGINT NOT NULL,
    indate DATETIME(6) NOT NULL,
    outeventid BINARY(16) NULL,
    outlaneid BIGINT NULL,
    outdate DATETIME(6) NULL,
    status VARCHAR(20) NOT NULL,
    PRIMARY KEY (xindex),
    UNIQUE KEY ux_parking_session_entry_event (ineventid),
    INDEX ix_parking_session_open_vehicle (sitenum, carnum, status, indate)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
