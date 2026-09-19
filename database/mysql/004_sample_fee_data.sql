INSERT INTO tparkfee
(sitenum,groupnum,weektype,dayshift,cartype,feestep,parktime,parkfee,maxcount)
VALUES
(1,1,1,0,1,1,30,0,1),
(1,1,1,0,1,2,10,200,3),
(1,1,1,0,1,3,10,300,0),
(1,1,2,0,1,1,30,0,1),
(1,1,2,0,1,2,10,200,3),
(1,1,2,0,1,3,10,300,0)
ON DUPLICATE KEY UPDATE
parktime=VALUES(parktime),parkfee=VALUES(parkfee),maxcount=VALUES(maxcount);

INSERT INTO tparkvariable(sitenum,groupnum,cmd_type,val,opt,msg)
VALUES
(1,1,'CMD_MAXDAILY_FEE','0','20000',NULL),
(1,1,'CMD_GRACE_TIME','0','30',NULL),
(1,1,'CMD_PREPAY_GRACE','0','10',NULL),
(1,1,'CMD_SERVICE_TIME','0','0',NULL),
(1,1,'CMD_DUPLICATE_ENTRY_TIME','0','10',NULL),
(1,1,'CMD_WEEKENDUSE','0','0',NULL),
(1,1,'CMD_HOLIDAYUSE','0','0',NULL),
(1,1,'CMD_OPTIME00',NULL,'00:00~23:59:59',NULL),
(1,1,'CMD_OPTIME01',NULL,'00:00~23:59:59',NULL),
(1,1,'CMD_OPTIME02',NULL,'00:00~23:59:59',NULL),
(1,1,'CMD_OPTIME03',NULL,'00:00~23:59:59',NULL),
(1,1,'CMD_OPTIME04',NULL,'00:00~23:59:59',NULL),
(1,1,'CMD_OPTIME05',NULL,'00:00~23:59:59',NULL),
(1,1,'CMD_OPTIME06',NULL,'00:00~23:59:59',NULL)
ON DUPLICATE KEY UPDATE val=VALUES(val),opt=VALUES(opt),msg=VALUES(msg);
