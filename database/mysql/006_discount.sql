ALTER TABLE payment
    DROP INDEX ux_payment_parkindex,
    ADD INDEX ix_payment_parkindex_paydate (parkindex, paydate);

CREATE TABLE vehicle_eligibility
(
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
    INDEX ix_vehicle_eligibility_lookup (sitenum, groupnum, carnum, benefittype, result, enddate),
    INDEX ix_vehicle_eligibility_expiration (expiredate)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE parking_session_discount
(
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
    CONSTRAINT fk_parking_session_discount_session FOREIGN KEY (parkindex) REFERENCES parking_session (xindex),
    CONSTRAINT fk_parking_session_discount_eligibility FOREIGN KEY (eligibilityindex) REFERENCES vehicle_eligibility (xindex)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
