ALTER TABLE payment
    DROP INDEX ux_payment_parking_session,
    ADD INDEX ix_payment_session_paid_at (parking_session_id, paid_at_utc);

CREATE TABLE vehicle_eligibility
(
    eligibility_id BIGINT NOT NULL AUTO_INCREMENT,
    request_id BINARY(16) NOT NULL,
    site_id BIGINT NOT NULL,
    group_id BIGINT NOT NULL,
    car_number VARCHAR(20) NOT NULL,
    eligibility_type VARCHAR(30) NOT NULL,
    provider VARCHAR(30) NOT NULL,
    provider_reference VARCHAR(64) NULL,
    verification_status VARCHAR(20) NOT NULL,
    discount_key INT NULL,
    discount_type SMALLINT NULL,
    discount_value INT NULL,
    valid_from DATE NULL,
    valid_to DATE NULL,
    checked_at_utc DATETIME(6) NOT NULL,
    expires_at_utc DATETIME(6) NULL,
    message VARCHAR(200) NULL,
    created_at_utc DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),

    PRIMARY KEY (eligibility_id),
    UNIQUE KEY ux_vehicle_eligibility_request (request_id),
    INDEX ix_vehicle_eligibility_lookup
        (site_id, group_id, car_number, eligibility_type, verification_status, valid_to),
    INDEX ix_vehicle_eligibility_expiration (expires_at_utc)
)
ENGINE=InnoDB
DEFAULT CHARSET=utf8mb4;

CREATE TABLE parking_session_discount
(
    parking_session_discount_id BIGINT NOT NULL AUTO_INCREMENT,
    parking_session_id BIGINT NOT NULL,
    car_number VARCHAR(20) NOT NULL,
    eligibility_id BIGINT NULL,
    discount_key INT NOT NULL,
    discount_source VARCHAR(20) NOT NULL,
    source_reference VARCHAR(64) NOT NULL,
    discount_type SMALLINT NOT NULL,
    discount_value INT NOT NULL,
    discount_amount BIGINT NOT NULL DEFAULT 0,
    registered_by VARCHAR(50) NULL,
    device_id BIGINT NULL,
    department_code INT NULL,
    remote_ip VARCHAR(45) NULL,
    registered_at_utc DATETIME(6) NOT NULL,
    applied_at_utc DATETIME(6) NULL,
    created_at_utc DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),

    PRIMARY KEY (parking_session_discount_id),
    UNIQUE KEY ux_parking_session_discount_source
        (parking_session_id, discount_source, source_reference),
    INDEX ix_parking_session_discount_session (parking_session_id, applied_at_utc),
    INDEX ix_parking_session_discount_car (car_number, registered_at_utc),
    INDEX ix_parking_session_discount_eligibility (eligibility_id),
    CONSTRAINT fk_parking_session_discount_session
        FOREIGN KEY (parking_session_id)
        REFERENCES parking_session (parking_session_id),
    CONSTRAINT fk_parking_session_discount_eligibility
        FOREIGN KEY (eligibility_id)
        REFERENCES vehicle_eligibility (eligibility_id)
)
ENGINE=InnoDB
DEFAULT CHARSET=utf8mb4;
