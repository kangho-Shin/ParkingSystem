CREATE TABLE IF NOT EXISTS tparkfee (
    sitenum INT NOT NULL,
    groupnum INT NOT NULL,
    weektype INT NOT NULL,
    dayshift INT NOT NULL DEFAULT 0,
    cartype INT NOT NULL,
    feestep INT NOT NULL,
    parktime INT NOT NULL,
    parkfee INT NOT NULL,
    maxcount INT NOT NULL DEFAULT 0,
    PRIMARY KEY (sitenum,groupnum,weektype,dayshift,cartype,feestep)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS tdiscount (
    sitenum INT NOT NULL,
    groupnum INT NOT NULL,
    `key` INT NOT NULL,
    type INT NOT NULL,
    value INT NULL,
    PRIMARY KEY (sitenum,groupnum,`key`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS tholiday (
    sitenum INT NOT NULL,
    groupnum INT NOT NULL,
    hdate DATE NOT NULL,
    PRIMARY KEY (sitenum,groupnum,hdate)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS tparkvariable (
    sitenum INT NOT NULL,
    groupnum INT NOT NULL,
    cmd_type VARCHAR(50) NOT NULL,
    val VARCHAR(255) NULL,
    opt VARCHAR(255) NULL,
    msg VARCHAR(255) NULL,
    PRIMARY KEY (sitenum,groupnum,cmd_type)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
