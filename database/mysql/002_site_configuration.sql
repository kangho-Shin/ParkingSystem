CREATE TABLE parking_site (
    site_id BIGINT NOT NULL,
    site_name VARCHAR(100) NOT NULL,
    enabled TINYINT(1) NOT NULL DEFAULT 1,
    updated_at_utc DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
    PRIMARY KEY (site_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE parking_lane (
    lane_id BIGINT NOT NULL,
    site_id BIGINT NOT NULL,
    group_number INT NOT NULL,
    lane_name VARCHAR(100) NOT NULL,
    direction VARCHAR(10) NOT NULL,
    enabled TINYINT(1) NOT NULL DEFAULT 1,
    updated_at_utc DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
    PRIMARY KEY (lane_id),
    UNIQUE KEY ux_parking_lane_site_group (site_id, group_number),
    CONSTRAINT fk_parking_lane_site FOREIGN KEY (site_id) REFERENCES parking_site(site_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE parking_device (
    device_id BIGINT NOT NULL,
    site_id BIGINT NOT NULL,
    lane_id BIGINT NULL,
    device_number INT NOT NULL,
    device_type VARCHAR(30) NOT NULL,
    device_name VARCHAR(100) NOT NULL,
    ip_address VARCHAR(45) NULL,
    enabled TINYINT(1) NOT NULL DEFAULT 1,
    updated_at_utc DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
    PRIMARY KEY (device_id),
    UNIQUE KEY ux_parking_device_site_number (site_id, device_number),
    INDEX ix_parking_device_lane (lane_id),
    CONSTRAINT fk_parking_device_site FOREIGN KEY (site_id) REFERENCES parking_site(site_id),
    CONSTRAINT fk_parking_device_lane FOREIGN KEY (lane_id) REFERENCES parking_lane(lane_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
