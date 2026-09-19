ALTER TABLE parking_session_discount
    ADD COLUMN car_number VARCHAR(20) NULL AFTER parking_session_id;

UPDATE parking_session_discount discount_info
JOIN parking_session session_info
  ON session_info.parking_session_id = discount_info.parking_session_id
SET discount_info.car_number = session_info.car_number
WHERE discount_info.car_number IS NULL;

ALTER TABLE parking_session_discount
    MODIFY COLUMN car_number VARCHAR(20) NOT NULL,
    ADD INDEX ix_parking_session_discount_car
        (car_number, registered_at_utc);
