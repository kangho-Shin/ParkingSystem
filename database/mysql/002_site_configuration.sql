CREATE TABLE IF NOT EXISTS parking_site (
    sitenum BIGINT NOT NULL,
    sitename VARCHAR(100) NOT NULL,
    useflag TINYINT(1) NOT NULL DEFAULT 1,
    updatedat DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
    PRIMARY KEY (sitenum)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS parking_lane (
    laneid BIGINT NOT NULL,
    sitenum BIGINT NOT NULL,
    groupnum INT NOT NULL,
    lanename VARCHAR(100) NOT NULL,
    direction VARCHAR(10) NOT NULL,
    useflag TINYINT(1) NOT NULL DEFAULT 1,
    updatedat DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
    PRIMARY KEY (laneid),
    INDEX ix_parking_lane_site_group (sitenum, groupnum),
    CONSTRAINT fk_parking_lane_site FOREIGN KEY (sitenum) REFERENCES parking_site(sitenum)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS parking_device (
    deviceid BIGINT NOT NULL,
    sitenum BIGINT NOT NULL,
    laneid BIGINT NULL,
    devicenum INT NOT NULL,
    devicetype VARCHAR(30) NOT NULL,
    devicename VARCHAR(100) NOT NULL,
    ipaddr VARCHAR(45) NULL,
    port INT NULL,
    useflag TINYINT(1) NOT NULL DEFAULT 1,
    updatedat DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
    PRIMARY KEY (deviceid),
    UNIQUE KEY ux_parking_device_site_number (sitenum, devicenum),
    INDEX ix_parking_device_lane (laneid),
    CONSTRAINT fk_parking_device_site FOREIGN KEY (sitenum) REFERENCES parking_site(sitenum),
    CONSTRAINT fk_parking_device_lane FOREIGN KEY (laneid) REFERENCES parking_lane(laneid)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS parking_device_link (
    sitenum BIGINT NOT NULL,
    sourcedeviceid BIGINT NOT NULL,
    targetdeviceid BIGINT NOT NULL,
    linktype VARCHAR(20) NOT NULL,
    useflag TINYINT(1) NOT NULL DEFAULT 1,
    PRIMARY KEY (sitenum,sourcedeviceid,targetdeviceid,linktype),
    CONSTRAINT fk_device_link_site FOREIGN KEY (sitenum) REFERENCES parking_site(sitenum),
    CONSTRAINT fk_device_link_source FOREIGN KEY (sourcedeviceid) REFERENCES parking_device(deviceid),
    CONSTRAINT fk_device_link_target FOREIGN KEY (targetdeviceid) REFERENCES parking_device(deviceid),
    CONSTRAINT chk_device_link_self CHECK (sourcedeviceid <> targetdeviceid)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
