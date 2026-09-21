ALTER TABLE parking_device
    ADD COLUMN linkeddeviceid BIGINT NULL AFTER port,
    ADD CONSTRAINT fk_parking_device_linked
        FOREIGN KEY (linkeddeviceid) REFERENCES parking_device(deviceid);
