CREATE TABLE parking_event
(
    event_id BINARY(16) NOT NULL,
    site_id BIGINT NOT NULL,
    lane_id BIGINT NOT NULL,
    device_id BIGINT NOT NULL,
    event_type VARCHAR(20) NOT NULL,
    car_number VARCHAR(20) NOT NULL,
    occurred_at_utc DATETIME(6) NOT NULL,
    result_json JSON NULL,
    created_at_utc DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),

    PRIMARY KEY (event_id),
    INDEX ix_parking_event_site_time (site_id, occurred_at_utc)
)
ENGINE=InnoDB
DEFAULT CHARSET=utf8mb4;

CREATE TABLE parking_session
(
    parking_session_id BIGINT NOT NULL AUTO_INCREMENT,
    site_id BIGINT NOT NULL,
    entry_event_id BINARY(16) NOT NULL,
    car_number VARCHAR(20) NOT NULL,
    entry_lane_id BIGINT NOT NULL,
    entry_at_utc DATETIME(6) NOT NULL,
    exit_event_id BINARY(16) NULL,
    exit_lane_id BIGINT NULL,
    exit_at_utc DATETIME(6) NULL,
    status VARCHAR(20) NOT NULL,

    PRIMARY KEY (parking_session_id),
    UNIQUE KEY ux_parking_session_entry_event (entry_event_id),
    INDEX ix_parking_session_open_vehicle
        (site_id, car_number, status, entry_at_utc)
)
ENGINE=InnoDB
DEFAULT CHARSET=utf8mb4;