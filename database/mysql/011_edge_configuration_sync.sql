CREATE TABLE IF NOT EXISTS parking_site_sync (
    sitenum BIGINT NOT NULL,
    authkeyhash CHAR(64) NOT NULL,
    configversion BIGINT NOT NULL DEFAULT 0,
    updatedat DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    PRIMARY KEY (sitenum),
    CONSTRAINT fk_parking_site_sync_site
        FOREIGN KEY (sitenum) REFERENCES parking_site(sitenum)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 현장 인증키는 중앙 관리자가 아래 값처럼 SHA-256 64자리 해시로 등록한다.
-- INSERT INTO parking_site_sync(sitenum,authkeyhash)
-- VALUES(9001,SHA2('현장-인증키',256));
