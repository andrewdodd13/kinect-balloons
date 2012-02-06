CREATE TABLE usercontent (
  ContentID SERIAL,
  Title VARCHAR(255),
  SubmittedBy VARCHAR(10),
  URL VARCHAR(255),
  Excerpt TEXT,
  Image BLOB,
  BalloonColour CHAR(6) DEFAULT FFFFFF,
  TimeCreated TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
PRIMARY KEY (ContentID))
ENGINE=InnoDB;