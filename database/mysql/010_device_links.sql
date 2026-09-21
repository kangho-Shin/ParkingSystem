CREATE TABLE IF NOT EXISTS parking_device_link (
    sitenum BIGINT NOT NULL,
    sourcedeviceid BIGINT NOT NULL,
    targetdeviceid BIGINT NOT NULL,
    linktype VARCHAR(20) NOT NULL,
    useflag TINYINT(1) NOT NULL DEFAULT 1,
    PRIMARY KEY (sitenum,sourcedeviceid,targetdeviceid,linktype),
    CONSTRAINT fk_device_link_site FOREIGN KEY (sitenum) REFERENCES parking_site(sitenum),
    CONSTRAINT fk_device_link_source FOREIGN KEY (sourcedeviceid) REFERENCES parking_device(deviceid),
    CONSTRAINT fk_device_link_target FOREIGN KEY (targetdeviceid) REFERENCES parking_device(deviceid),
    CONSTRAINT chk_device_link_self CHECK (sourcedeviceid <> targetdeviceid)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

SET @has_linkeddeviceid = (
    SELECT COUNT(*) FROM information_schema.columns
    WHERE table_schema=DATABASE() AND table_name='parking_device' AND column_name='linkeddeviceid');
SET @copy_links_sql = IF(@has_linkeddeviceid > 0,
    'INSERT IGNORE INTO parking_device_link(sitenum,sourcedeviceid,targetdeviceid,linktype,useflag) SELECT sitenum,linkeddeviceid,deviceid,UPPER(devicetype),useflag FROM parking_device WHERE linkeddeviceid IS NOT NULL',
    'SELECT 1');
PREPARE copy_links FROM @copy_links_sql;
EXECUTE copy_links;
DEALLOCATE PREPARE copy_links;

SET @has_link_fk = (
    SELECT COUNT(*) FROM information_schema.table_constraints
    WHERE constraint_schema=DATABASE() AND table_name='parking_device'
      AND constraint_name='fk_parking_device_linked' AND constraint_type='FOREIGN KEY');
SET @drop_link_fk_sql = IF(@has_link_fk > 0,
    'ALTER TABLE parking_device DROP FOREIGN KEY fk_parking_device_linked', 'SELECT 1');
PREPARE drop_link_fk FROM @drop_link_fk_sql;
EXECUTE drop_link_fk;
DEALLOCATE PREPARE drop_link_fk;

SET @drop_link_column_sql = IF(@has_linkeddeviceid > 0,
    'ALTER TABLE parking_device DROP COLUMN linkeddeviceid', 'SELECT 1');
PREPARE drop_link_column FROM @drop_link_column_sql;
EXECUTE drop_link_column;
DEALLOCATE PREPARE drop_link_column;

INSERT INTO tparkvariable(sitenum,groupnum,cmd_type,val,opt,msg)
SELECT sitenum,groupnum,'CMD_KIOSK_OFFLINE_POLICY','OPEN',NULL,NULL
FROM parking_lane
GROUP BY sitenum,groupnum
ON DUPLICATE KEY UPDATE cmd_type=VALUES(cmd_type);
