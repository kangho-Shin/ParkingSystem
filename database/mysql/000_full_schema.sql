-- ParkingSystem 신규 설치용 통합 스키마
-- 이 파일을 실행한 신규 DB에는 001~007을 다시 실행하지 않는다.

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
    CONSTRAINT fk_parking_lane_site
        FOREIGN KEY (sitenum) REFERENCES parking_site(sitenum)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS parking_device (
    deviceid BIGINT NOT NULL,
    sitenum BIGINT NOT NULL,
    laneid BIGINT NULL,
    devicenum INT NOT NULL,
    devicetype VARCHAR(30) NOT NULL,
    devicename VARCHAR(100) NOT NULL,
    ipaddr VARCHAR(45) NULL,
    useflag TINYINT(1) NOT NULL DEFAULT 1,
    updatedat DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
    PRIMARY KEY (deviceid),
    UNIQUE KEY ux_parking_device_site_number (sitenum, devicenum),
    INDEX ix_parking_device_lane (laneid),
    CONSTRAINT fk_parking_device_site
        FOREIGN KEY (sitenum) REFERENCES parking_site(sitenum),
    CONSTRAINT fk_parking_device_lane
        FOREIGN KEY (laneid) REFERENCES parking_lane(laneid)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS parking_event (
    eventid BINARY(16) NOT NULL,
    sitenum BIGINT NOT NULL,
    groupnum INT NOT NULL DEFAULT 1,
    laneid BIGINT NOT NULL,
    deviceid BIGINT NOT NULL,
    eventtype VARCHAR(20) NOT NULL,
    carnum VARCHAR(20) NOT NULL,
    eventat DATETIME(6) NOT NULL,
    imagepath VARCHAR(255) NULL,
    resultjson JSON NULL,
    createdat DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    PRIMARY KEY (eventid),
    INDEX ix_parking_event_site_time (sitenum, eventat),
    INDEX ix_parking_event_vehicle (sitenum, groupnum, carnum, eventat)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS parking_session (
    xindex BIGINT NOT NULL AUTO_INCREMENT,
    sitenum BIGINT NOT NULL,
    ineventid BINARY(16) NOT NULL,
    carnum VARCHAR(20) NOT NULL,
    groupnum INT NOT NULL DEFAULT 1,
    cartype INT NOT NULL DEFAULT 1,
    inlaneid BIGINT NOT NULL,
    indeviceid BIGINT NULL,
    indate DATETIME(6) NOT NULL,
    inimage VARCHAR(255) NULL,
    paydate DATETIME(6) NULL,
    outeventid BINARY(16) NULL,
    outlaneid BIGINT NULL,
    outdeviceid BIGINT NULL,
    outdate DATETIME(6) NULL,
    outimage VARCHAR(255) NULL,
    outflag CHAR(1) NOT NULL DEFAULT 'I',
    PRIMARY KEY (xindex),
    UNIQUE KEY ux_parking_session_entry_event (ineventid),
    INDEX ix_parking_session_open_vehicle (sitenum, groupnum, carnum, outflag, indate),
    CONSTRAINT chk_parking_session_outflag CHECK (outflag IN ('I','X','O'))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS tparkfee (
    sitenum INT NOT NULL,
    groupnum INT NOT NULL,
    weektype INT NOT NULL,
    dayshift INT NOT NULL DEFAULT 0,
    cartype INT NOT NULL,
    feestep INT NOT NULL,
    parktime INT NOT NULL,
    parkfee INT NOT NULL,
    maxcount INT NOT NULL DEFAULT 0,
    PRIMARY KEY (sitenum, groupnum, weektype, dayshift, cartype, feestep)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS tdiscount (
    sitenum INT NOT NULL,
    groupnum INT NOT NULL,
    `key` INT NOT NULL,
    type INT NOT NULL,
    value INT NULL,
    PRIMARY KEY (sitenum, groupnum, `key`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS tholiday (
    sitenum INT NOT NULL,
    groupnum INT NOT NULL,
    hdate DATE NOT NULL,
    PRIMARY KEY (sitenum, groupnum, hdate)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS tparkvariable (
    sitenum INT NOT NULL,
    groupnum INT NOT NULL,
    cmd_type VARCHAR(50) NOT NULL,
    val VARCHAR(255) NULL,
    opt VARCHAR(255) NULL,
    msg VARCHAR(255) NULL,
    PRIMARY KEY (sitenum, groupnum, cmd_type)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS payment (
    paymentid BINARY(16) NOT NULL,
    parkindex BIGINT NOT NULL,
    sitenum BIGINT NOT NULL,
    originalfee BIGINT NOT NULL,
    discountfee BIGINT NOT NULL,
    payamount BIGINT NOT NULL,
    paymethod VARCHAR(20) NOT NULL,
    approvalnum VARCHAR(50) NOT NULL,
    terminalid VARCHAR(50) NULL,
    paydate DATETIME(6) NOT NULL,
    createdat DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    PRIMARY KEY (paymentid),
    INDEX ix_payment_parkindex_paydate (parkindex, paydate),
    INDEX ix_payment_site_paydate (sitenum, paydate),
    CONSTRAINT fk_payment_parking_session
        FOREIGN KEY (parkindex) REFERENCES parking_session(xindex)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS vehicle_eligibility (
    xindex BIGINT NOT NULL AUTO_INCREMENT,
    requestid BINARY(16) NOT NULL,
    sitenum BIGINT NOT NULL,
    groupnum BIGINT NOT NULL,
    carnum VARCHAR(20) NOT NULL,
    benefittype VARCHAR(30) NOT NULL,
    provider VARCHAR(30) NOT NULL,
    providerref VARCHAR(64) NULL,
    result VARCHAR(20) NOT NULL,
    discountkey INT NULL,
    discounttype SMALLINT NULL,
    discountvalue INT NULL,
    startdate DATE NULL,
    enddate DATE NULL,
    checkdate DATETIME(6) NOT NULL,
    expiredate DATETIME(6) NULL,
    msg VARCHAR(200) NULL,
    createdat DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    PRIMARY KEY (xindex),
    UNIQUE KEY ux_vehicle_eligibility_request (requestid),
    INDEX ix_vehicle_eligibility_lookup
        (sitenum, groupnum, carnum, benefittype, result, enddate),
    INDEX ix_vehicle_eligibility_expiration (expiredate)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS parking_session_discount (
    xindex BIGINT NOT NULL AUTO_INCREMENT,
    parkindex BIGINT NOT NULL,
    carnum VARCHAR(20) NOT NULL,
    eligibilityindex BIGINT NULL,
    discountkey INT NOT NULL,
    source VARCHAR(20) NOT NULL,
    sourceref VARCHAR(64) NOT NULL,
    discounttype SMALLINT NOT NULL,
    discountvalue INT NOT NULL,
    discountmoney BIGINT NOT NULL DEFAULT 0,
    logid VARCHAR(50) NULL,
    devicenum BIGINT NULL,
    deptcode INT NULL,
    remoteip VARCHAR(45) NULL,
    sdate DATETIME(6) NOT NULL,
    applydate DATETIME(6) NULL,
    createdat DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    PRIMARY KEY (xindex),
    UNIQUE KEY ux_parking_session_discount_source (parkindex, source, sourceref),
    INDEX ix_parking_session_discount_park (parkindex, applydate),
    INDEX ix_parking_session_discount_car (carnum, sdate),
    INDEX ix_parking_session_discount_eligibility (eligibilityindex),
    CONSTRAINT fk_parking_session_discount_session
        FOREIGN KEY (parkindex) REFERENCES parking_session(xindex),
    CONSTRAINT fk_parking_session_discount_eligibility
        FOREIGN KEY (eligibilityindex) REFERENCES vehicle_eligibility(xindex)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 등록차량 회원 원본. 기존 운영 필드를 유지한다.
CREATE TABLE IF NOT EXISTS tperiodmember (
    xindex BIGINT NOT NULL AUTO_INCREMENT,
    sitenum BIGINT NOT NULL DEFAULT 1,
    groupnum INT NOT NULL DEFAULT 1,
    devicenum INT NOT NULL DEFAULT 0,
    cardid BIGINT NOT NULL DEFAULT 0,
    serialno VARCHAR(20) NULL,
    periodtype SMALLINT NOT NULL DEFAULT 0,
    name VARCHAR(30) NULL,
    telnum VARCHAR(20) NULL,
    groupcode VARCHAR(20) NULL,
    company1 VARCHAR(80) NOT NULL DEFAULT '',
    company2 VARCHAR(80) NOT NULL DEFAULT '',
    carnum1 VARCHAR(20) NOT NULL,
    cartype1 VARCHAR(30) NOT NULL DEFAULT '',
    carnum2 VARCHAR(20) NOT NULL DEFAULT '',
    cartype2 VARCHAR(30) NOT NULL DEFAULT '',
    address VARCHAR(100) NOT NULL DEFAULT '',
    parktype SMALLINT NOT NULL DEFAULT 0,
    recorddate DATE NULL,
    startdate DATE NULL,
    enddate DATE NULL,
    parktimecode SMALLINT NOT NULL DEFAULT 0,
    parktimetime VARCHAR(20) NULL,
    parkprice INT NOT NULL DEFAULT 0,
    parkarea VARCHAR(8) NULL,
    parklevel INT NOT NULL DEFAULT 0,
    parkvalidday VARCHAR(8) NULL,
    antiflag SMALLINT NOT NULL DEFAULT 0,
    useflag SMALLINT NOT NULL DEFAULT 0,
    serviceday SMALLINT NOT NULL DEFAULT 0,
    managercode SMALLINT NOT NULL DEFAULT 0,
    managername VARCHAR(30) NULL,
    paytype VARCHAR(45) NULL,
    outflag CHAR(1) NOT NULL DEFAULT 'I',
    intimetick INT NULL,
    reserved1 SMALLINT NOT NULL DEFAULT 0,
    reserved2 SMALLINT NOT NULL DEFAULT 0,
    reserved3 SMALLINT NOT NULL DEFAULT 0,
    reserved4 SMALLINT NOT NULL DEFAULT 0,
    note VARCHAR(50) NULL,
    PRIMARY KEY (xindex),
    INDEX ix_periodmember_site_car1 (sitenum, carnum1),
    INDEX ix_periodmember_cardid (cardid),
    INDEX ix_periodmember_serialno (serialno),
    INDEX ix_periodmember_car2 (sitenum, carnum2),
    INDEX ix_periodmember_validity (sitenum, useflag, startdate, enddate)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 등록차량 입출차. 날짜/시/분 분리 대신 DATETIME(6)을 사용한다.
CREATE TABLE IF NOT EXISTS tperiodinout (
    xindex BIGINT NOT NULL AUTO_INCREMENT,
    sitenum BIGINT NOT NULL DEFAULT 1,
    groupnum INT NOT NULL DEFAULT 1,
    memberindex BIGINT NULL,
    cardid BIGINT NOT NULL DEFAULT 0,
    name VARCHAR(30) NULL,
    carnum VARCHAR(20) NOT NULL,
    cartype VARCHAR(30) NOT NULL DEFAULT '',
    ineventid BINARY(16) NOT NULL,
    inlaneid BIGINT NOT NULL,
    indeviceid BIGINT NOT NULL,
    indatetime DATETIME(6) NOT NULL,
    inimage VARCHAR(255) NULL,
    outeventid BINARY(16) NULL,
    outlaneid BIGINT NULL,
    outdeviceid BIGINT NULL,
    outdatetime DATETIME(6) NULL,
    outimage VARCHAR(255) NULL,
    parktime INT NOT NULL DEFAULT 0,
    enddate DATE NULL,
    managercode SMALLINT NOT NULL DEFAULT 0,
    managername VARCHAR(30) NULL,
    outflag CHAR(1) NOT NULL DEFAULT 'I',
    note VARCHAR(45) NULL,
    parktimecode SMALLINT NULL,
    parktimetime VARCHAR(20) NULL,
    backimage VARCHAR(255) NULL,
    createdat DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    PRIMARY KEY (xindex),
    UNIQUE KEY ux_periodinout_entry_event (ineventid),
    INDEX ix_periodinout_open_vehicle
        (sitenum, groupnum, carnum, outflag, indatetime),
    INDEX ix_periodinout_member (memberindex, indatetime),
    CONSTRAINT chk_periodinout_outflag CHECK (outflag IN ('I','X','O')),
    CONSTRAINT fk_periodinout_member
        FOREIGN KEY (memberindex) REFERENCES tperiodmember(xindex)
        ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 기본 요금 및 운영변수 시험 데이터
INSERT INTO tparkfee
(sitenum,groupnum,weektype,dayshift,cartype,feestep,parktime,parkfee,maxcount)
VALUES
(1,1,1,0,1,1,30,0,1),
(1,1,1,0,1,2,10,200,3),
(1,1,1,0,1,3,10,300,0),
(1,1,2,0,1,1,30,0,1),
(1,1,2,0,1,2,10,200,3),
(1,1,2,0,1,3,10,300,0)
ON DUPLICATE KEY UPDATE
parktime=VALUES(parktime),parkfee=VALUES(parkfee),maxcount=VALUES(maxcount);

INSERT INTO tparkvariable(sitenum,groupnum,cmd_type,val,opt,msg)
VALUES
(1,1,'CMD_MAXDAILY_FEE','0','20000',NULL),
(1,1,'CMD_GRACE_TIME','0','30',NULL),
(1,1,'CMD_PREPAY_GRACE','0','10',NULL),
(1,1,'CMD_SERVICE_TIME','0','0',NULL),
(1,1,'CMD_DUPLICATE_ENTRY_TIME','0','10',NULL),
(1,1,'CMD_WEEKENDUSE','0','0',NULL),
(1,1,'CMD_HOLIDAYUSE','0','0',NULL),
(1,1,'CMD_OPTIME00',NULL,'00:00~23:59:59',NULL),
(1,1,'CMD_OPTIME01',NULL,'00:00~23:59:59',NULL),
(1,1,'CMD_OPTIME02',NULL,'00:00~23:59:59',NULL),
(1,1,'CMD_OPTIME03',NULL,'00:00~23:59:59',NULL),
(1,1,'CMD_OPTIME04',NULL,'00:00~23:59:59',NULL),
(1,1,'CMD_OPTIME05',NULL,'00:00~23:59:59',NULL),
(1,1,'CMD_OPTIME06',NULL,'00:00~23:59:59',NULL)
ON DUPLICATE KEY UPDATE val=VALUES(val),opt=VALUES(opt),msg=VALUES(msg);
