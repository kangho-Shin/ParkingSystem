-- 시험 현장 9001 장치 식별자를 종류별 범위로 이전한다.
-- 기존 테이블과 운전 데이터는 보존하며 여러 번 실행할 수 있다.

CREATE TEMPORARY TABLE IF NOT EXISTS device_id_map (
    olddeviceid BIGINT PRIMARY KEY,
    newdeviceid BIGINT NOT NULL UNIQUE,
    newdevicenum INT NOT NULL UNIQUE,
    expectedtype VARCHAR(30) NOT NULL,
    laneid BIGINT NULL,
    devicename VARCHAR(100) NOT NULL
);

DELETE FROM device_id_map;
INSERT INTO device_id_map VALUES
(9301, 2001, 201, 'KIOSK', 9020, '출구무인'),
(9101, 4001, 401, 'LPR',   9010, '입차LPR'),
(9201, 4002, 402, 'LPR',   9020, '출차LPR'),
(9401, 5001, 501, 'LDM',   9020, '출구전광판');

SET @device_collision_count = (
    SELECT COUNT(*)
    FROM parking_device d
    WHERE (d.deviceid IN (9301,2001)
           AND (d.sitenum<>9001 OR UPPER(d.devicetype)<>'KIOSK'))
       OR (d.deviceid IN (9101,4001,9201,4002)
           AND (d.sitenum<>9001 OR UPPER(d.devicetype)<>'LPR'))
       OR (d.deviceid IN (9401,5001)
           AND (d.sitenum<>9001 OR UPPER(d.devicetype)<>'LDM'))
       OR (d.sitenum=9001 AND d.devicenum IN (201,401,402,501)
           AND d.deviceid NOT IN (9301,2001,9101,4001,9201,4002,9401,5001))
       OR (d.deviceid IN (4003,4004)
           AND (d.sitenum<>9001 OR UPPER(d.devicetype)<>'LPR'))
       OR (d.sitenum=9001 AND d.devicenum IN (403,404)
           AND d.deviceid NOT IN (4003,4004))
);
SET @device_collision_sql = IF(
    @device_collision_count=0,
    'SELECT 1',
    'SIGNAL SQLSTATE ''45000'' SET MESSAGE_TEXT=''device identity range collision''');
PREPARE device_collision_stmt FROM @device_collision_sql;
EXECUTE device_collision_stmt;
DEALLOCATE PREPARE device_collision_stmt;

START TRANSACTION;

INSERT INTO parking_site(sitenum,sitename,useflag)
VALUES(9001,'시험현장',1)
ON DUPLICATE KEY UPDATE useflag=VALUES(useflag);

INSERT INTO parking_lane(laneid,sitenum,groupnum,lanename,direction,useflag) VALUES
(9010,9001,2,'입차차로','Entry',1),
(9020,9001,2,'출차차로','Exit',1)
ON DUPLICATE KEY UPDATE sitenum=VALUES(sitenum),groupnum=VALUES(groupnum),
lanename=VALUES(lanename),direction=VALUES(direction),useflag=VALUES(useflag);

UPDATE parking_device d
JOIN device_id_map m ON d.deviceid=m.olddeviceid
SET d.devicenum=-m.newdevicenum
WHERE d.sitenum=9001;

INSERT INTO parking_device(
    deviceid,sitenum,laneid,devicenum,devicetype,devicename,ipaddr,port,useflag)
SELECT m.newdeviceid,9001,m.laneid,m.newdevicenum,m.expectedtype,m.devicename,
       COALESCE(old.ipaddr,current.ipaddr),COALESCE(old.port,current.port),
       COALESCE(old.useflag,current.useflag,1)
FROM device_id_map m
LEFT JOIN parking_device old ON old.deviceid=m.olddeviceid AND old.sitenum=9001
LEFT JOIN parking_device current ON current.deviceid=m.newdeviceid AND current.sitenum=9001
ON DUPLICATE KEY UPDATE laneid=VALUES(laneid),devicenum=VALUES(devicenum),
devicetype=VALUES(devicetype),devicename=VALUES(devicename),ipaddr=VALUES(ipaddr),
port=VALUES(port),useflag=VALUES(useflag),updatedat=NOW(6);

INSERT INTO parking_device(
    deviceid,sitenum,laneid,devicenum,devicetype,devicename,ipaddr,port,useflag) VALUES
(4003,9001,9010,403,'LPR','보조입차LPR',NULL,29200,0),
(4004,9001,9020,404,'LPR','보조출차LPR',NULL,29200,0)
ON DUPLICATE KEY UPDATE laneid=VALUES(laneid),devicenum=VALUES(devicenum),
devicetype=VALUES(devicetype),devicename=VALUES(devicename),useflag=0,updatedat=NOW(6);

SET @migration_sql = IF(
    EXISTS(SELECT 1 FROM information_schema.tables WHERE table_schema=DATABASE() AND table_name='parking_event'),
    'UPDATE parking_event e JOIN device_id_map m ON e.deviceid=m.olddeviceid SET e.deviceid=m.newdeviceid WHERE e.sitenum=9001',
    'SELECT 1');
PREPARE migration_stmt FROM @migration_sql;
EXECUTE migration_stmt;
DEALLOCATE PREPARE migration_stmt;

SET @migration_sql = IF(
    EXISTS(SELECT 1 FROM information_schema.tables WHERE table_schema=DATABASE() AND table_name='parking_session'),
    'UPDATE parking_session s JOIN device_id_map m ON s.indeviceid=m.olddeviceid SET s.indeviceid=m.newdeviceid WHERE s.sitenum=9001',
    'SELECT 1');
PREPARE migration_stmt FROM @migration_sql;
EXECUTE migration_stmt;
DEALLOCATE PREPARE migration_stmt;

SET @migration_sql = IF(
    EXISTS(SELECT 1 FROM information_schema.tables WHERE table_schema=DATABASE() AND table_name='parking_session'),
    'UPDATE parking_session s JOIN device_id_map m ON s.outdeviceid=m.olddeviceid SET s.outdeviceid=m.newdeviceid WHERE s.sitenum=9001',
    'SELECT 1');
PREPARE migration_stmt FROM @migration_sql;
EXECUTE migration_stmt;
DEALLOCATE PREPARE migration_stmt;

SET @migration_sql = IF(
    EXISTS(SELECT 1 FROM information_schema.columns WHERE table_schema=DATABASE() AND table_name='tperiodinout' AND column_name='indeviceid'),
    'UPDATE tperiodinout p JOIN device_id_map m ON p.indeviceid=m.olddeviceid SET p.indeviceid=m.newdeviceid WHERE p.sitenum=9001',
    IF(EXISTS(SELECT 1 FROM information_schema.columns WHERE table_schema=DATABASE() AND table_name='tperiodinout' AND column_name='indevicenum'),
       'UPDATE tperiodinout SET indevicenum=CASE indevicenum WHEN 101 THEN 401 WHEN 201 THEN 402 ELSE indevicenum END WHERE sitenum=9001 AND indevicenum IN (101,201)',
       'SELECT 1'));
PREPARE migration_stmt FROM @migration_sql;
EXECUTE migration_stmt;
DEALLOCATE PREPARE migration_stmt;

SET @migration_sql = IF(
    EXISTS(SELECT 1 FROM information_schema.columns WHERE table_schema=DATABASE() AND table_name='tperiodinout' AND column_name='outdeviceid'),
    'UPDATE tperiodinout p JOIN device_id_map m ON p.outdeviceid=m.olddeviceid SET p.outdeviceid=m.newdeviceid WHERE p.sitenum=9001',
    IF(EXISTS(SELECT 1 FROM information_schema.columns WHERE table_schema=DATABASE() AND table_name='tperiodinout' AND column_name='outdevicenum'),
       'UPDATE tperiodinout SET outdevicenum=CASE outdevicenum WHEN 101 THEN 401 WHEN 201 THEN 402 ELSE outdevicenum END WHERE sitenum=9001 AND outdevicenum IN (101,201)',
       'SELECT 1'));
PREPARE migration_stmt FROM @migration_sql;
EXECUTE migration_stmt;
DEALLOCATE PREPARE migration_stmt;

INSERT INTO parking_device_link(sitenum,sourcedeviceid,targetdeviceid,linktype,useflag)
SELECT l.sitenum,
       CASE l.sourcedeviceid WHEN 9301 THEN 2001 WHEN 9101 THEN 4001
            WHEN 9201 THEN 4002 WHEN 9401 THEN 5001 ELSE l.sourcedeviceid END,
       CASE l.targetdeviceid WHEN 9301 THEN 2001 WHEN 9101 THEN 4001
            WHEN 9201 THEN 4002 WHEN 9401 THEN 5001 ELSE l.targetdeviceid END,
       l.linktype,l.useflag
FROM parking_device_link l
WHERE l.sitenum=9001
  AND (l.sourcedeviceid IN (9301,9101,9201,9401)
       OR l.targetdeviceid IN (9301,9101,9201,9401))
ON DUPLICATE KEY UPDATE useflag=VALUES(useflag);

DELETE l FROM parking_device_link l
WHERE l.sitenum=9001
  AND (l.sourcedeviceid IN (9301,9101,9201,9401)
       OR l.targetdeviceid IN (9301,9101,9201,9401));

DELETE d FROM parking_device d
JOIN device_id_map m ON m.olddeviceid=d.deviceid
WHERE d.sitenum=9001;

UPDATE parking_site_sync
SET configversion=configversion+1,updatedat=NOW(6)
WHERE sitenum=9001;

COMMIT;
DROP TEMPORARY TABLE device_id_map;
