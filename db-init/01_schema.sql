-- Denne kjøres automatisk første gang containeren starter
CREATE TABLE IF NOT EXISTS obstacles (
  id             INT AUTO_INCREMENT PRIMARY KEY,
  center_lat     DOUBLE NOT NULL,
  center_lng     DOUBLE NOT NULL,
  radius_m       INT NOT NULL,
  type           VARCHAR(50) NOT NULL,
  height_min_m   INT NOT NULL,
  height_max_m   INT NULL,
  created_utc    DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
