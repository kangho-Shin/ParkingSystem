ALTER TABLE parking_session
    ADD COLUMN paid_at_utc DATETIME(6) NULL AFTER entry_at_utc;

CREATE TABLE payment
(
    payment_id BINARY(16) NOT NULL,
    parking_session_id BIGINT NOT NULL,
    site_id BIGINT NOT NULL,
    original_fee BIGINT NOT NULL,
    discount_fee BIGINT NOT NULL,
    paid_amount BIGINT NOT NULL,
    payment_method VARCHAR(20) NOT NULL,
    approval_number VARCHAR(50) NOT NULL,
    terminal_id VARCHAR(50) NULL,
    paid_at_utc DATETIME(6) NOT NULL,
    created_at_utc DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),

    PRIMARY KEY (payment_id),
    UNIQUE KEY ux_payment_parking_session (parking_session_id),
    INDEX ix_payment_site_paid_at (site_id, paid_at_utc),
    CONSTRAINT fk_payment_parking_session
        FOREIGN KEY (parking_session_id)
        REFERENCES parking_session (parking_session_id)
)
ENGINE=InnoDB
DEFAULT CHARSET=utf8mb4;
