ALTER TABLE parking_session ADD COLUMN paydate DATETIME(6) NULL AFTER indate;

CREATE TABLE IF NOT EXISTS payment
(
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
    UNIQUE KEY ux_payment_parkindex (parkindex),
    INDEX ix_payment_site_paydate (sitenum, paydate),
    CONSTRAINT fk_payment_parking_session FOREIGN KEY (parkindex) REFERENCES parking_session (xindex)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
