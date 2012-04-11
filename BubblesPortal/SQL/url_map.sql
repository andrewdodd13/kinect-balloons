CREATE TABLE `url_map` (
  `id` bigint(20) unsigned NOT NULL auto_increment,
  `url` varchar(255) NOT NULL,
  PRIMARY KEY  (`id`),
  UNIQUE KEY `id` (`id`),
  UNIQUE KEY `url` (`url`)
) ENGINE=MyISAM DEFAULT CHARSET=utf8;
