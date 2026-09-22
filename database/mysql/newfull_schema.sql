-- ParkingSystem 통합 신규 설치용 스키마
-- MySQL 5.7 / InnoDB / utf8mb4
-- 기존 000_full_schema.sql은 유지하며 이 파일과 함께 실행하지 않는다.

SET NAMES utf8mb4;

CREATE TABLE IF NOT EXISTS tparkings (
    xindex BIGINT NOT NULL AUTO_INCREMENT,
    sitenum BIGINT NOT NULL,
    groupnum INT NOT NULL,
    parkname VARCHAR(100) NOT NULL,
    parktype VARCHAR(20) NOT NULL DEFAULT '',
    addr VARCHAR(200) NULL,
    tel VARCHAR(30) NULL,
    boss VARCHAR(30) NULL,
    sitekeyhash CHAR(64) NOT NULL,
    useflag TINYINT NOT NULL DEFAULT 1,
    regdate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    moddate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (xindex),
    UNIQUE KEY ux_parkings_site_group (sitenum,groupnum)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS tlaneinfo (
    laneid BIGINT NOT NULL,
    sitenum BIGINT NOT NULL,
    groupnum INT NOT NULL,
    lanename VARCHAR(100) NOT NULL,
    direction VARCHAR(10) NOT NULL,
    useflag TINYINT NOT NULL DEFAULT 1,
    regdate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    moddate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    note VARCHAR(200) NULL,
    PRIMARY KEY (laneid),
    KEY ix_laneinfo_site_group (sitenum,groupnum),
    CONSTRAINT fk_laneinfo_parkings FOREIGN KEY (sitenum,groupnum)
        REFERENCES tparkings(sitenum,groupnum)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS tdeviceinfo (
    deviceid BIGINT NOT NULL,
    sitenum BIGINT NOT NULL,
    groupnum INT NOT NULL,
    laneid BIGINT NULL,
    devicenum INT NOT NULL,
    devicename VARCHAR(100) NOT NULL,
    devicetype INT NOT NULL DEFAULT 0,
    ip VARCHAR(45) NULL,
    port INT NULL,
    protocol VARCHAR(10) NULL,
    termid VARCHAR(30) NULL,
    posid VARCHAR(30) NULL,
    useflag TINYINT NOT NULL DEFAULT 1,
    regdate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    moddate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    note VARCHAR(200) NULL,
    PRIMARY KEY (deviceid),
    UNIQUE KEY ux_deviceinfo_site_group_num (sitenum,groupnum,devicenum),
    KEY ix_deviceinfo_lane (laneid),
    CONSTRAINT fk_deviceinfo_parkings FOREIGN KEY (sitenum,groupnum)
        REFERENCES tparkings(sitenum,groupnum),
    CONSTRAINT fk_deviceinfo_lane FOREIGN KEY (laneid)
        REFERENCES tlaneinfo(laneid)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS tdevicelink (
    xindex BIGINT NOT NULL AUTO_INCREMENT,
    sitenum BIGINT NOT NULL,
    groupnum INT NOT NULL,
    sourcedeviceid BIGINT NOT NULL,
    targetdeviceid BIGINT NOT NULL,
    linktype VARCHAR(20) NOT NULL,
    useflag TINYINT NOT NULL DEFAULT 1,
    regdate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    moddate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (xindex),
    UNIQUE KEY ux_devicelink_source_target_type (sourcedeviceid,targetdeviceid,linktype),
    KEY ix_devicelink_site_group (sitenum,groupnum),
    CONSTRAINT fk_devicelink_source FOREIGN KEY (sourcedeviceid)
        REFERENCES tdeviceinfo(deviceid),
    CONSTRAINT fk_devicelink_target FOREIGN KEY (targetdeviceid)
        REFERENCES tdeviceinfo(deviceid)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS tparksync (
    xindex BIGINT NOT NULL AUTO_INCREMENT,
    sitenum BIGINT NOT NULL,
    deviceid BIGINT NOT NULL,
    configversion BIGINT NOT NULL DEFAULT 0,
    appliedversion BIGINT NOT NULL DEFAULT 0,
    lastrequestdate DATETIME NULL,
    lastsyncdate DATETIME NULL,
    status VARCHAR(10) NOT NULL DEFAULT 'WAIT',
    msg VARCHAR(255) NULL,
    moddate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (xindex),
    UNIQUE KEY ux_parksync_site_device (sitenum,deviceid),
    CONSTRAINT fk_parksync_device FOREIGN KEY (deviceid)
        REFERENCES tdeviceinfo(deviceid)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS tparkfee (
    xindex BIGINT NOT NULL AUTO_INCREMENT,
    sitenum BIGINT NOT NULL,
    groupnum INT NOT NULL,
    ptype INT NOT NULL DEFAULT 0,
    pname VARCHAR(100) NULL,
    weektype INT NOT NULL DEFAULT 0,
    dayshift INT NOT NULL DEFAULT 0,
    cartype INT NOT NULL DEFAULT 0,
    feestep INT NOT NULL DEFAULT 0,
    parktime INT NOT NULL DEFAULT 0,
    parkfee INT NOT NULL DEFAULT 0,
    maxcount INT NOT NULL DEFAULT 0,
    periodtype INT NOT NULL DEFAULT 0,
    periodname VARCHAR(100) NULL,
    useflag TINYINT NOT NULL DEFAULT 1,
    mid VARCHAR(20) NULL,
    modmid VARCHAR(20) NULL,
    regdate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    moddate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (xindex),
    UNIQUE KEY ux_parkfee_rule
        (sitenum,groupnum,ptype,weektype,dayshift,cartype,feestep,periodtype),
    KEY ix_parkfee_lookup (sitenum,groupnum,ptype,useflag)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS tdiscount (
    xindex BIGINT NOT NULL AUTO_INCREMENT,
    sitenum BIGINT NOT NULL,
    groupnum INT NOT NULL,
    diskey INT NOT NULL,
    distype INT NOT NULL,
    disvalue INT NULL,
    title VARCHAR(100) NOT NULL,
    maxcount INT NOT NULL DEFAULT 0,
    opt VARCHAR(255) NULL,
    useflag TINYINT NOT NULL DEFAULT 1,
    mid VARCHAR(20) NULL,
    regdate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    moddate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (xindex),
    UNIQUE KEY ux_discount_site_group_key (sitenum,groupnum,diskey)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS tholiday (
    xindex BIGINT NOT NULL AUTO_INCREMENT,
    sitenum BIGINT NOT NULL,
    groupnum INT NOT NULL,
    holiday DATE NOT NULL,
    msg VARCHAR(100) NULL,
    useflag TINYINT NOT NULL DEFAULT 1,
    mid VARCHAR(20) NULL,
    regdate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    moddate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (xindex),
    UNIQUE KEY ux_holiday_site_group_date (sitenum,groupnum,holiday)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS tparkvariable (
    xindex BIGINT NOT NULL AUTO_INCREMENT,
    sitenum BIGINT NOT NULL,
    groupnum INT NOT NULL,
    cmdtype VARCHAR(50) NOT NULL,
    val VARCHAR(255) NULL,
    opt VARCHAR(255) NULL,
    msg VARCHAR(255) NULL,
    useflag TINYINT NOT NULL DEFAULT 1,
    mid VARCHAR(20) NULL,
    mname VARCHAR(30) NULL,
    regdate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    moddate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (xindex),
    UNIQUE KEY ux_parkvariable_site_group_cmd (sitenum,groupnum,cmdtype)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS tperiodparktime (
    xindex BIGINT NOT NULL AUTO_INCREMENT,
    sitenum BIGINT NOT NULL,
    groupnum INT NOT NULL,
    code INT NOT NULL,
    description VARCHAR(100) NOT NULL,
    starttime TIME NOT NULL,
    endtime TIME NOT NULL,
    useflag TINYINT NOT NULL DEFAULT 1,
    mid VARCHAR(20) NULL,
    mname VARCHAR(30) NULL,
    regdate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    moddate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    note VARCHAR(200) NULL,
    PRIMARY KEY (xindex),
    UNIQUE KEY ux_periodparktime_site_group_code (sitenum,groupnum,code)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS tcompany (
    xindex BIGINT NOT NULL AUTO_INCREMENT,
    sitenum BIGINT NOT NULL,
    groupnum INT NOT NULL,
    companycode INT NOT NULL,
    companyname VARCHAR(80) NOT NULL,
    boss VARCHAR(30) NULL,
    addr VARCHAR(200) NULL,
    tel VARCHAR(30) NULL,
    fax VARCHAR(30) NULL,
    useflag TINYINT NOT NULL DEFAULT 1,
    mid VARCHAR(20) NULL,
    mname VARCHAR(30) NULL,
    regdate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    moddate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    note VARCHAR(200) NULL,
    PRIMARY KEY (xindex),
    UNIQUE KEY ux_company_site_group_code (sitenum,groupnum,companycode)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS tdepartment (
    xindex BIGINT NOT NULL AUTO_INCREMENT,
    sitenum BIGINT NOT NULL,
    groupnum INT NOT NULL,
    companycode INT NOT NULL,
    deptcode INT NOT NULL,
    deptname VARCHAR(80) NOT NULL,
    tel VARCHAR(30) NULL,
    fax VARCHAR(30) NULL,
    useflag TINYINT NOT NULL DEFAULT 1,
    mid VARCHAR(20) NULL,
    mname VARCHAR(30) NULL,
    regdate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    moddate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    note VARCHAR(200) NULL,
    PRIMARY KEY (xindex),
    UNIQUE KEY ux_department_site_group_code
        (sitenum,groupnum,companycode,deptcode)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS tmanager (
    xindex BIGINT NOT NULL AUTO_INCREMENT,
    sitenum BIGINT NOT NULL,
    groupnum INT NOT NULL,
    mid VARCHAR(20) NOT NULL,
    mpwhash VARCHAR(255) NOT NULL,
    grade INT NOT NULL DEFAULT 1,
    name VARCHAR(30) NOT NULL,
    tel VARCHAR(30) NULL,
    dept VARCHAR(80) NULL,
    useflag TINYINT NOT NULL DEFAULT 1,
    lastlogindate DATETIME NULL,
    regdate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    moddate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (xindex),
    UNIQUE KEY ux_manager_site_group_id (sitenum,groupnum,mid)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS tdisaccount (
    xindex BIGINT NOT NULL AUTO_INCREMENT,
    sitenum BIGINT NOT NULL,
    groupnum INT NOT NULL,
    id VARCHAR(20) NOT NULL,
    passhash VARCHAR(255) NOT NULL,
    name VARCHAR(30) NOT NULL,
    tel VARCHAR(30) NULL,
    donghosu VARCHAR(20) NULL,
    deptcode INT NULL,
    grade INT NOT NULL DEFAULT 1,
    useflag TINYINT NOT NULL DEFAULT 1,
    lastlogindate DATETIME NULL,
    regdate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    moddate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (xindex),
    UNIQUE KEY ux_disaccount_site_group_id (sitenum,groupnum,id),
    KEY ix_disaccount_dept (sitenum,groupnum,deptcode)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS tdisdept (
    xindex BIGINT NOT NULL AUTO_INCREMENT,
    sitenum BIGINT NOT NULL,
    groupnum INT NOT NULL,
    deptcode INT NOT NULL,
    deptname VARCHAR(80) NOT NULL,
    donghosu VARCHAR(20) NULL,
    maxcount INT NOT NULL DEFAULT 0,
    carmaxcount INT NOT NULL DEFAULT 0,
    useflag TINYINT NOT NULL DEFAULT 1,
    regdate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    moddate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (xindex),
    UNIQUE KEY ux_disdept_site_group_code (sitenum,groupnum,deptcode)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS talarmcode (
    xindex BIGINT NOT NULL AUTO_INCREMENT,
    devicetype INT NOT NULL,
    errcode INT NOT NULL,
    errname VARCHAR(100) NOT NULL,
    errgrade INT NOT NULL DEFAULT 1,
    useflag TINYINT NOT NULL DEFAULT 1,
    regdate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    moddate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (xindex),
    UNIQUE KEY ux_alarmcode_device_error (devicetype,errcode)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS tticketnum (
    xindex BIGINT NOT NULL AUTO_INCREMENT,
    sitenum BIGINT NOT NULL,
    groupnum INT NOT NULL,
    devicenum INT NOT NULL,
    ticketnum INT NOT NULL DEFAULT 0,
    minnum INT NOT NULL DEFAULT 1,
    maxnum INT NOT NULL DEFAULT 99999,
    moddate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (xindex),
    UNIQUE KEY ux_ticketnum_site_group_device (sitenum,groupnum,devicenum)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS tparkingnum (
    xindex BIGINT NOT NULL AUTO_INCREMENT,
    sitenum BIGINT NOT NULL,
    groupnum INT NOT NULL,
    inilbancnt BIGINT NOT NULL DEFAULT 0,
    outilbancnt BIGINT NOT NULL DEFAULT 0,
    inregcnt BIGINT NOT NULL DEFAULT 0,
    outregcnt BIGINT NOT NULL DEFAULT 0,
    ilbanfullnum INT NOT NULL DEFAULT 0,
    regfullnum INT NOT NULL DEFAULT 0,
    moddate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (xindex),
    UNIQUE KEY ux_parkingnum_site_group (sitenum,groupnum)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS tperiodmember (
    xindex BIGINT NOT NULL AUTO_INCREMENT,
    sitenum BIGINT NOT NULL,
    groupnum INT NOT NULL,
    devicenum INT NOT NULL DEFAULT 0,
    cardno BIGINT NOT NULL DEFAULT 0,
    serialno VARCHAR(20) NULL,
    periodtype INT NOT NULL DEFAULT 0,
    groupcode INT NOT NULL DEFAULT 0,
    carnum1 VARCHAR(20) NOT NULL,
    cartype1 INT NOT NULL DEFAULT 1,
    carnum2 VARCHAR(20) NULL,
    cartype2 INT NULL,
    name VARCHAR(30) NOT NULL,
    tel VARCHAR(30) NULL,
    addr VARCHAR(200) NULL,
    companycode INT NULL,
    deptcode INT NULL,
    startdate DATE NOT NULL,
    enddate DATE NOT NULL,
    serviceday INT NOT NULL DEFAULT 0,
    parktype INT NOT NULL DEFAULT 0,
    parktimecode INT NOT NULL DEFAULT 0,
    parkarea CHAR(7) NOT NULL DEFAULT '0000000',
    parkvalidday CHAR(7) NOT NULL DEFAULT '1111111',
    parklevel INT NOT NULL DEFAULT 0,
    parkprice INT NOT NULL DEFAULT 0,
    diskey INT NULL,
    paytype INT NOT NULL DEFAULT 0,
    useflag TINYINT NOT NULL DEFAULT 1,
    outflag CHAR(1) NOT NULL DEFAULT 'O',
    mid VARCHAR(20) NULL,
    mname VARCHAR(30) NULL,
    regdate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    moddate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (xindex),
    UNIQUE KEY ux_periodmember_site_car1 (sitenum,carnum1),
    KEY ix_periodmember_card (sitenum,cardno),
    KEY ix_periodmember_car2 (sitenum,carnum2),
    KEY ix_periodmember_validity (sitenum,useflag,startdate,enddate)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS tbangmun (
    xindex BIGINT NOT NULL AUTO_INCREMENT,
    sitenum BIGINT NOT NULL,
    groupnum INT NOT NULL,
    cardid BIGINT NOT NULL DEFAULT 0,
    carnum VARCHAR(20) NOT NULL,
    name VARCHAR(30) NULL,
    tel VARCHAR(30) NULL,
    visitobject VARCHAR(100) NULL,
    visitplace VARCHAR(100) NULL,
    startdate DATE NOT NULL,
    enddate DATE NOT NULL,
    diskey INT NULL,
    reservedtype INT NOT NULL DEFAULT 0,
    useflag TINYINT NOT NULL DEFAULT 1,
    mid VARCHAR(20) NULL,
    mname VARCHAR(30) NULL,
    regdate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    moddate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (xindex),
    KEY ix_bangmun_lookup (sitenum,groupnum,carnum,useflag,startdate,enddate)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS twelfare (
    xindex BIGINT NOT NULL AUTO_INCREMENT,
    sitenum BIGINT NOT NULL,
    groupnum INT NOT NULL,
    welfareid VARCHAR(64) NULL,
    welfaretype INT NOT NULL,
    carnum VARCHAR(20) NOT NULL,
    name VARCHAR(30) NULL,
    tel VARCHAR(30) NULL,
    startdate DATE NOT NULL,
    enddate DATE NOT NULL,
    diskey INT NOT NULL,
    useflag TINYINT NOT NULL DEFAULT 1,
    mid VARCHAR(20) NULL,
    regdate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    moddate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (xindex),
    KEY ix_welfare_lookup (sitenum,groupnum,carnum,useflag,startdate,enddate),
    KEY ix_welfare_external (welfareid)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS tparkevent (
    xindex BIGINT NOT NULL AUTO_INCREMENT,
    eventid CHAR(32) NOT NULL,
    sitenum BIGINT NOT NULL,
    groupnum INT NOT NULL,
    laneid BIGINT NOT NULL,
    deviceid BIGINT NOT NULL,
    devicenum INT NOT NULL,
    eventtype VARCHAR(10) NOT NULL,
    carnum VARCHAR(20) NULL,
    eventdate DATETIME NOT NULL,
    image VARCHAR(255) NULL,
    backimage VARCHAR(255) NULL,
    data JSON NULL,
    status VARCHAR(10) NOT NULL DEFAULT 'RECEIVED',
    parktype VARCHAR(10) NULL,
    pindex BIGINT NULL,
    resultcode VARCHAR(30) NULL,
    msg VARCHAR(255) NULL,
    receivedate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    completedate DATETIME NULL,
    PRIMARY KEY (xindex),
    UNIQUE KEY ux_parkevent_eventid (eventid),
    KEY ix_parkevent_site_date (sitenum,eventdate),
    KEY ix_parkevent_vehicle (sitenum,groupnum,carnum,eventdate),
    CONSTRAINT fk_parkevent_lane FOREIGN KEY (laneid)
        REFERENCES tlaneinfo(laneid),
    CONSTRAINT fk_parkevent_device FOREIGN KEY (deviceid)
        REFERENCES tdeviceinfo(deviceid)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS tparkinfo (
    xindex BIGINT NOT NULL AUTO_INCREMENT,
    sitenum BIGINT NOT NULL,
    groupnum INT NOT NULL,
    ineventid CHAR(32) NOT NULL,
    outeventid CHAR(32) NULL,
    ticketnum INT NULL,
    ticketdata VARCHAR(30) NULL,
    carnum VARCHAR(20) NOT NULL,
    cartype INT NOT NULL DEFAULT 1,
    parktype INT NOT NULL DEFAULT 0,
    visitindex BIGINT NULL,
    inlaneid BIGINT NOT NULL,
    indevicenum INT NOT NULL,
    indate DATETIME NOT NULL,
    inimage VARCHAR(255) NULL,
    inbackimage VARCHAR(255) NULL,
    paydate DATETIME NULL,
    parktime INT NOT NULL DEFAULT 0,
    parkfee INT NOT NULL DEFAULT 0,
    discountfee INT NOT NULL DEFAULT 0,
    discounttime INT NOT NULL DEFAULT 0,
    payfee INT NOT NULL DEFAULT 0,
    paidfee INT NOT NULL DEFAULT 0,
    paytype INT NOT NULL DEFAULT 0,
    prepay TINYINT NOT NULL DEFAULT 0,
    outlaneid BIGINT NULL,
    outdevicenum INT NULL,
    outdate DATETIME NULL,
    outimage VARCHAR(255) NULL,
    outbackimage VARCHAR(255) NULL,
    parkonplace VARCHAR(30) NULL,
    outflag CHAR(1) NOT NULL DEFAULT 'I',
    manual TINYINT NOT NULL DEFAULT 0,
    manflag TINYINT NOT NULL DEFAULT 0,
    dendflag TINYINT NOT NULL DEFAULT 0,
    tendflag TINYINT NOT NULL DEFAULT 0,
    denddate DATETIME NULL,
    mid VARCHAR(20) NULL,
    mname VARCHAR(30) NULL,
    regdate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    moddate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (xindex),
    UNIQUE KEY ux_parkinfo_ineventid (ineventid),
    UNIQUE KEY ux_parkinfo_outeventid (outeventid),
    KEY ix_parkinfo_open_vehicle (sitenum,groupnum,carnum,outflag,indate),
    KEY ix_parkinfo_ticketdata (ticketdata),
    KEY ix_parkinfo_indate (sitenum,groupnum,indate),
    CONSTRAINT fk_parkinfo_visit FOREIGN KEY (visitindex)
        REFERENCES tbangmun(xindex)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS tperiodinout (
    xindex BIGINT NOT NULL AUTO_INCREMENT,
    periodindex BIGINT NOT NULL,
    sitenum BIGINT NOT NULL,
    groupnum INT NOT NULL,
    ineventid CHAR(32) NOT NULL,
    outeventid CHAR(32) NULL,
    cardno BIGINT NOT NULL DEFAULT 0,
    name VARCHAR(30) NULL,
    enddate DATE NULL,
    carnum VARCHAR(20) NOT NULL,
    cartype INT NOT NULL DEFAULT 1,
    inlaneid BIGINT NOT NULL,
    indevicenum INT NOT NULL,
    indate DATETIME NOT NULL,
    inimage VARCHAR(255) NULL,
    inbackimage VARCHAR(255) NULL,
    outlaneid BIGINT NULL,
    outdevicenum INT NULL,
    outdate DATETIME NULL,
    outimage VARCHAR(255) NULL,
    outbackimage VARCHAR(255) NULL,
    parktime INT NOT NULL DEFAULT 0,
    parkonplace VARCHAR(30) NULL,
    outflag CHAR(1) NOT NULL DEFAULT 'I',
    manual TINYINT NOT NULL DEFAULT 0,
    mid VARCHAR(20) NULL,
    mname VARCHAR(30) NULL,
    note VARCHAR(100) NULL,
    regdate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    moddate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (xindex),
    UNIQUE KEY ux_periodinout_ineventid (ineventid),
    UNIQUE KEY ux_periodinout_outeventid (outeventid),
    KEY ix_periodinout_open_vehicle (sitenum,groupnum,carnum,outflag,indate),
    KEY ix_periodinout_member (periodindex,indate),
    CONSTRAINT fk_periodinout_member FOREIGN KEY (periodindex)
        REFERENCES tperiodmember(xindex)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS tbcardinfo (
    xindex BIGINT NOT NULL AUTO_INCREMENT,
    paymentid CHAR(32) NOT NULL,
    orgpaymentid CHAR(32) NULL,
    pindex BIGINT NOT NULL,
    sitenum BIGINT NOT NULL,
    groupnum INT NOT NULL,
    devicenum INT NOT NULL,
    carnum VARCHAR(20) NULL,
    termid VARCHAR(30) NULL,
    posid VARCHAR(30) NULL,
    dealtype VARCHAR(10) NOT NULL,
    credittype INT NOT NULL DEFAULT 0,
    money INT NOT NULL,
    rescode VARCHAR(10) NULL,
    msg VARCHAR(100) NULL,
    dealnum VARCHAR(30) NULL,
    acceptnum VARCHAR(30) NULL,
    receiptnum VARCHAR(30) NULL,
    cardname VARCHAR(30) NULL,
    cardno VARCHAR(30) NULL,
    branchnum VARCHAR(30) NULL,
    dealdate DATETIME NOT NULL,
    regdate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (xindex),
    UNIQUE KEY ux_cardinfo_paymentid (paymentid),
    KEY ix_cardinfo_orgpaymentid (orgpaymentid),
    KEY ix_cardinfo_park (pindex,dealdate),
    KEY ix_cardinfo_site_date (sitenum,dealdate)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS tdiscountinfo (
    xindex BIGINT NOT NULL AUTO_INCREMENT,
    discountid CHAR(32) NOT NULL,
    pindex BIGINT NOT NULL,
    sitenum BIGINT NOT NULL,
    groupnum INT NOT NULL,
    carnum VARCHAR(20) NOT NULL,
    diskey INT NOT NULL,
    distype INT NOT NULL,
    disvalue INT NOT NULL,
    disfee INT NOT NULL DEFAULT 0,
    source VARCHAR(20) NOT NULL,
    sourceref VARCHAR(64) NOT NULL,
    id VARCHAR(20) NULL,
    deptcode INT NULL,
    devicenum INT NULL,
    ip VARCHAR(45) NULL,
    image VARCHAR(255) NULL,
    indate DATETIME NOT NULL,
    disdate DATETIME NOT NULL,
    regdate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (xindex),
    UNIQUE KEY ux_discountinfo_discountid (discountid),
    UNIQUE KEY ux_discountinfo_source (pindex,source,sourceref),
    KEY ix_discountinfo_vehicle (sitenum,groupnum,carnum,disdate),
    KEY ix_discountinfo_account (sitenum,groupnum,id,diskey,disdate),
    CONSTRAINT fk_discountinfo_park FOREIGN KEY (pindex)
        REFERENCES tparkinfo(xindex)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS tperiodaccount (
    xindex BIGINT NOT NULL AUTO_INCREMENT,
    accountid CHAR(32) NOT NULL,
    orgaccountid CHAR(32) NULL,
    periodindex BIGINT NOT NULL,
    sitenum BIGINT NOT NULL,
    groupnum INT NOT NULL,
    devicenum INT NOT NULL DEFAULT 0,
    devicetype INT NOT NULL DEFAULT 0,
    cardno BIGINT NOT NULL DEFAULT 0,
    carnum VARCHAR(20) NOT NULL,
    name VARCHAR(30) NULL,
    companycode INT NULL,
    deptcode INT NULL,
    accounttype VARCHAR(10) NOT NULL,
    beforedate DATE NULL,
    startdate DATE NOT NULL,
    enddate DATE NOT NULL,
    money INT NOT NULL,
    paytype INT NOT NULL DEFAULT 0,
    paymentid CHAR(32) NULL,
    mid VARCHAR(20) NULL,
    mname VARCHAR(30) NULL,
    regdate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (xindex),
    UNIQUE KEY ux_periodaccount_accountid (accountid),
    KEY ix_periodaccount_original (orgaccountid),
    KEY ix_periodaccount_member (periodindex,regdate),
    CONSTRAINT fk_periodaccount_member FOREIGN KEY (periodindex)
        REFERENCES tperiodmember(xindex)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS taccountinfo (
    xindex BIGINT NOT NULL AUTO_INCREMENT,
    sitenum BIGINT NOT NULL,
    groupnum INT NOT NULL,
    id VARCHAR(20) NOT NULL,
    diskey INT NOT NULL,
    nowcount INT NOT NULL DEFAULT 0,
    maxcount INT NOT NULL DEFAULT 0,
    lastresetdate DATETIME NULL,
    regdate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    moddate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (xindex),
    UNIQUE KEY ux_accountinfo_site_group_id_key (sitenum,groupnum,id,diskey)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS tperiodwaiting (
    xindex BIGINT NOT NULL AUTO_INCREMENT,
    sitenum BIGINT NOT NULL,
    groupnum INT NOT NULL,
    periodtype INT NOT NULL,
    carnum VARCHAR(20) NOT NULL,
    cartype INT NOT NULL DEFAULT 1,
    name VARCHAR(30) NOT NULL,
    tel VARCHAR(30) NULL,
    waitingdate DATETIME NOT NULL,
    waitingno INT NOT NULL,
    parkarea CHAR(7) NOT NULL DEFAULT '0000000',
    status VARCHAR(10) NOT NULL DEFAULT 'WAIT',
    periodindex BIGINT NULL,
    mid VARCHAR(20) NULL,
    mname VARCHAR(30) NULL,
    regdate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    moddate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (xindex),
    KEY ix_periodwaiting_order (sitenum,groupnum,periodtype,status,waitingno),
    KEY ix_periodwaiting_vehicle (sitenum,carnum)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS tnorecognition (
    xindex BIGINT NOT NULL AUTO_INCREMENT,
    eventid CHAR(32) NOT NULL,
    sitenum BIGINT NOT NULL,
    groupnum INT NOT NULL,
    laneid BIGINT NOT NULL,
    devicenum INT NOT NULL,
    devicename VARCHAR(100) NULL,
    ip VARCHAR(45) NULL,
    iodate DATETIME NOT NULL,
    image VARCHAR(255) NULL,
    backimage VARCHAR(255) NULL,
    regdate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (xindex),
    UNIQUE KEY ux_norecognition_eventid (eventid),
    KEY ix_norecognition_site_date (sitenum,groupnum,iodate)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS tblacklist (
    xindex BIGINT NOT NULL AUTO_INCREMENT,
    sitenum BIGINT NOT NULL,
    groupnum INT NOT NULL,
    carnum VARCHAR(20) NOT NULL,
    startdate DATE NOT NULL,
    enddate DATE NULL,
    msg VARCHAR(200) NULL,
    useflag TINYINT NOT NULL DEFAULT 1,
    mid VARCHAR(20) NULL,
    mname VARCHAR(30) NULL,
    regdate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    moddate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (xindex),
    KEY ix_blacklist_lookup (sitenum,groupnum,carnum,useflag,startdate,enddate)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS tdevicealarm (
    xindex BIGINT NOT NULL AUTO_INCREMENT,
    alarmid CHAR(32) NOT NULL,
    sitenum BIGINT NOT NULL,
    groupnum INT NOT NULL,
    deviceid BIGINT NOT NULL,
    devicenum INT NOT NULL,
    devicename VARCHAR(100) NOT NULL,
    devicetype INT NOT NULL,
    ip VARCHAR(45) NULL,
    errcode INT NOT NULL,
    alarmkind VARCHAR(20) NULL,
    alarmtime DATETIME NOT NULL,
    repairtime DATETIME NULL,
    alarmmessage VARCHAR(255) NULL,
    repairmessage VARCHAR(255) NULL,
    mid VARCHAR(20) NULL,
    mname VARCHAR(30) NULL,
    PRIMARY KEY (xindex),
    UNIQUE KEY ux_devicealarm_alarmid (alarmid),
    KEY ix_devicealarm_active (deviceid,errcode,repairtime),
    CONSTRAINT fk_devicealarm_device FOREIGN KEY (deviceid)
        REFERENCES tdeviceinfo(deviceid)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS tvaninfo (
    xindex BIGINT NOT NULL AUTO_INCREMENT,
    sitenum BIGINT NOT NULL,
    groupnum INT NOT NULL,
    vancode VARCHAR(30) NOT NULL,
    vanname VARCHAR(50) NOT NULL,
    branchcode VARCHAR(30) NOT NULL,
    branchname VARCHAR(80) NULL,
    useflag TINYINT NOT NULL DEFAULT 1,
    regdate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    moddate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (xindex),
    UNIQUE KEY ux_vaninfo_site_group_branch (sitenum,groupnum,vancode,branchcode)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS tapsinfo (
    xindex BIGINT NOT NULL AUTO_INCREMENT,
    sitenum BIGINT NOT NULL,
    groupnum INT NOT NULL,
    apsip VARCHAR(45) NULL,
    devicenum INT NOT NULL,
    pindex BIGINT NULL,
    indatetime DATETIME NULL,
    outdatetime DATETIME NULL,
    totalfee INT NOT NULL DEFAULT 0,
    disfee INT NOT NULL DEFAULT 0,
    calfee INT NOT NULL DEFAULT 0,
    distime INT NOT NULL DEFAULT 0,
    status INT NOT NULL DEFAULT 0,
    PRIMARY KEY (xindex),
    UNIQUE KEY ux_apsinfo_site_group_device (sitenum,groupnum,devicenum)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS tgateinfo (
    xindex BIGINT NOT NULL AUTO_INCREMENT,
    commandid CHAR(32) NOT NULL,
    sitenum BIGINT NOT NULL,
    groupnum INT NOT NULL,
    devicenum INT NOT NULL,
    command VARCHAR(10) NOT NULL,
    status VARCHAR(10) NOT NULL DEFAULT 'WAIT',
    mid VARCHAR(20) NULL,
    mname VARCHAR(30) NULL,
    regdate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    completedate DATETIME NULL,
    msg VARCHAR(255) NULL,
    PRIMARY KEY (xindex),
    UNIQUE KEY ux_gateinfo_commandid (commandid),
    KEY ix_gateinfo_wait (sitenum,status,regdate)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS tlogon (
    xindex BIGINT NOT NULL AUTO_INCREMENT,
    sitenum BIGINT NOT NULL,
    groupnum INT NOT NULL,
    devicenum INT NULL,
    devicename VARCHAR(100) NULL,
    devicetype INT NULL,
    mid VARCHAR(20) NULL,
    mname VARCHAR(30) NULL,
    ip VARCHAR(45) NULL,
    logdatetime DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    logtype INT NOT NULL,
    PRIMARY KEY (xindex),
    KEY ix_logon_site_date (sitenum,groupnum,logdatetime),
    KEY ix_logon_manager (mid,logdatetime)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 시험현장 9001 / 그룹 2
INSERT INTO tparkings
(sitenum,groupnum,parkname,parktype,sitekeyhash,useflag)
VALUES
(9001,2,'시험현장','TEST','1407e87efd8347ff0be25f5d3273f73578199d34f665f6be1ab4c920ea1431e3',1)
ON DUPLICATE KEY UPDATE parkname=VALUES(parkname),parktype=VALUES(parktype),
sitekeyhash=VALUES(sitekeyhash),useflag=VALUES(useflag);

INSERT INTO tlaneinfo
(laneid,sitenum,groupnum,lanename,direction,useflag) VALUES
(9010,9001,2,'입차차로','ENTRY',1),
(9020,9001,2,'출차차로','EXIT',1)
ON DUPLICATE KEY UPDATE sitenum=VALUES(sitenum),groupnum=VALUES(groupnum),
lanename=VALUES(lanename),direction=VALUES(direction),useflag=VALUES(useflag);

INSERT INTO tdeviceinfo
(deviceid,sitenum,groupnum,laneid,devicenum,devicename,devicetype,ip,port,protocol,useflag) VALUES
(2001,9001,2,9020,201,'출구무인',2,NULL,NULL,'HTTP',1),
(4001,9001,2,9010,401,'입차LPR',3,NULL,29200,'TCP',1),
(4002,9001,2,9020,402,'출차LPR',3,NULL,29200,'TCP',1),
(5001,9001,2,9020,501,'출구전광판',6,'192.168.0.151',35000,'TCP',1),
(6001,9001,2,9020,601,'출구차단기',7,NULL,NULL,'TCP',1),
(8001,9001,2,NULL,801,'현장Edge',8,NULL,5200,'HTTP',1)
ON DUPLICATE KEY UPDATE laneid=VALUES(laneid),devicenum=VALUES(devicenum),
devicename=VALUES(devicename),devicetype=VALUES(devicetype),ip=VALUES(ip),
port=VALUES(port),protocol=VALUES(protocol),useflag=VALUES(useflag);

INSERT INTO tdevicelink
(sitenum,groupnum,sourcedeviceid,targetdeviceid,linktype,useflag) VALUES
(9001,2,4002,2001,'KIOSK',1),
(9001,2,4002,5001,'LDM',1),
(9001,2,2001,5001,'LDM',1),
(9001,2,2001,6001,'GATE',1)
ON DUPLICATE KEY UPDATE useflag=VALUES(useflag);

INSERT INTO tparksync
(sitenum,deviceid,configversion,appliedversion,status)
VALUES(9001,8001,1,0,'WAIT')
ON DUPLICATE KEY UPDATE configversion=VALUES(configversion),status=VALUES(status);

INSERT INTO tparkingnum
(sitenum,groupnum,inilbancnt,outilbancnt,inregcnt,outregcnt,ilbanfullnum,regfullnum)
VALUES(9001,2,0,0,0,0,0,0)
ON DUPLICATE KEY UPDATE ilbanfullnum=VALUES(ilbanfullnum),regfullnum=VALUES(regfullnum);

INSERT INTO tperiodparktime
(sitenum,groupnum,code,description,starttime,endtime,useflag)
VALUES(9001,2,0,'전일','00:00:00','23:59:59',1)
ON DUPLICATE KEY UPDATE description=VALUES(description),starttime=VALUES(starttime),
endtime=VALUES(endtime),useflag=VALUES(useflag);

INSERT INTO tparkfee
(sitenum,groupnum,ptype,pname,weektype,dayshift,cartype,feestep,parktime,parkfee,maxcount,periodtype,periodname,useflag)
VALUES
(9001,2,0,'평일 일반요금',1,0,1,1,30,0,1,0,NULL,1),
(9001,2,0,'평일 일반요금',1,0,1,2,10,200,3,0,NULL,1),
(9001,2,0,'평일 일반요금',1,0,1,3,10,300,0,0,NULL,1),
(9001,2,0,'주말 일반요금',2,0,1,1,30,0,1,0,NULL,1),
(9001,2,0,'주말 일반요금',2,0,1,2,10,200,3,0,NULL,1),
(9001,2,0,'주말 일반요금',2,0,1,3,10,300,0,0,NULL,1)
ON DUPLICATE KEY UPDATE pname=VALUES(pname),parktime=VALUES(parktime),
parkfee=VALUES(parkfee),maxcount=VALUES(maxcount),useflag=VALUES(useflag);

INSERT INTO tdiscount
(sitenum,groupnum,diskey,distype,disvalue,title,maxcount,opt,useflag) VALUES
(9001,2,10,4,50,'50원 고정',1,NULL,1),
(9001,2,20,1,30,'30분 할인',0,NULL,1),
(9001,2,30,2,1000,'1000원 할인',0,NULL,1),
(9001,2,40,3,1000,'퍼센트 할인',0,NULL,1)
ON DUPLICATE KEY UPDATE distype=VALUES(distype),disvalue=VALUES(disvalue),
title=VALUES(title),maxcount=VALUES(maxcount),opt=VALUES(opt),useflag=VALUES(useflag);

INSERT INTO tparkvariable
(sitenum,groupnum,cmdtype,val,opt,msg,useflag) VALUES
(9001,2,'CMD_MAXDAILY_FEE','0','20000',NULL,1),
(9001,2,'CMD_GRACE_TIME','0','30',NULL,1),
(9001,2,'CMD_PREPAY_GRACE','0','10',NULL,1),
(9001,2,'CMD_SERVICE_TIME','0','0',NULL,1),
(9001,2,'CMD_DUPLICATE_ENTRY_TIME','0','10',NULL,1),
(9001,2,'CMD_WEEKENDUSE','0','0',NULL,1),
(9001,2,'CMD_HOLIDAYUSE','0','0',NULL,1),
(9001,2,'CMD_KIOSK_OFFLINE_POLICY','OPEN',NULL,NULL,1),
(9001,2,'CMD_CONTROL_MODE','KIOSK',NULL,NULL,1),
(9001,2,'CMD_LPR_PORT','29200',NULL,NULL,1),
(9001,2,'CMD_OPTIME00',NULL,'00:00~23:59:59',NULL,1),
(9001,2,'CMD_OPTIME01',NULL,'00:00~23:59:59',NULL,1),
(9001,2,'CMD_OPTIME02',NULL,'00:00~23:59:59',NULL,1),
(9001,2,'CMD_OPTIME03',NULL,'00:00~23:59:59',NULL,1),
(9001,2,'CMD_OPTIME04',NULL,'00:00~23:59:59',NULL,1),
(9001,2,'CMD_OPTIME05',NULL,'00:00~23:59:59',NULL,1),
(9001,2,'CMD_OPTIME06',NULL,'00:00~23:59:59',NULL,1)
ON DUPLICATE KEY UPDATE val=VALUES(val),opt=VALUES(opt),msg=VALUES(msg),
useflag=VALUES(useflag);
