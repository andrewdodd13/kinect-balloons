CREATE TABLE `usercontent` (
  `ContentID` bigint(20) unsigned NOT NULL auto_increment,
  `Title` varchar(255) default NULL,
  `SubmittedBy` varchar(10) default NULL,
  `URL` varchar(255) default NULL,
  `Excerpt` mediumtext,
  `TimeCreated` timestamp NOT NULL default CURRENT_TIMESTAMP,
  `BalloonColour` char(6) default 'FFFFFF',
  `ImageURL` varchar(255) default NULL,
  `Votes` int(11) NOT NULL default '0',
  `GivenName` varchar(50) default NULL,
  PRIMARY KEY  (`ContentID`),
  UNIQUE KEY `ContentID` (`ContentID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
