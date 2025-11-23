-- ============================================
-- NRL database: domene-tabeller
-- (Identity-tabellene lages av EF/Identity)
-- ============================================

-- 1) ORGANIZATIONS
--    Brukes for å koble hindre / brukere til NLA, Luftforsvaret, Politiet osv.
CREATE TABLE IF NOT EXISTS organizations (
  id           INT AUTO_INCREMENT PRIMARY KEY,
  name         VARCHAR(200) NOT NULL,
  code         VARCHAR(50)  NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Seed noen standardorganisasjoner (bare hvis tabellen er tom)
INSERT INTO organizations (name, code)
SELECT * FROM (
  SELECT 'Norsk Luftambulanse', 'NLA' UNION ALL
  SELECT 'Luftforsvaret',       'LUFT' UNION ALL
  SELECT 'Politiets helikoptertjeneste', 'POLITI'
) AS tmp
WHERE NOT EXISTS (SELECT 1 FROM organizations);

-- 2) OBSTACLES
--    Alle innmeldte hindre fra piloter
CREATE TABLE IF NOT EXISTS obstacles (
  id                   INT AUTO_INCREMENT PRIMARY KEY,
  geojson              LONGTEXT      NOT NULL,
  obstacle_name        VARCHAR(200)  NULL,
  obstacle_category    VARCHAR(100)  NULL, 
  height_m             INT           NULL,
  obstacle_description TEXT          NULL,
  is_draft             TINYINT(1)    NOT NULL DEFAULT 0,
  created_utc          DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,

  -- Nye felter for kravene:
  -- Hvem meldte inn, hvem behandler, status, kommentar, organisasjon
  created_by_user_id   VARCHAR(255)  NULL,
  assigned_to_user_id  VARCHAR(255)  NULL,
  review_status        VARCHAR(50)   NULL,
  review_comment       TEXT          NULL,
  organization_id      INT           NULL,

  -- (valgfri FK i logisk forstand; vi dropper hard FK mot AspNetUsers
  --  for å unngå rekkefølgeproblemer med Identity-migrasjoner)
  CONSTRAINT fk_obstacles_org
    FOREIGN KEY (organization_id) REFERENCES organizations(id)
      ON DELETE SET NULL
      ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Indexer for raskere søk/filtrering
CREATE INDEX IF NOT EXISTS idx_obstacles_created_utc
  ON obstacles (created_utc);

CREATE INDEX IF NOT EXISTS idx_obstacles_org
  ON obstacles (organization_id);

CREATE INDEX IF NOT EXISTS idx_obstacles_created_by
  ON obstacles (created_by_user_id);

CREATE INDEX IF NOT EXISTS idx_obstacles_status
  ON obstacles (review_status);

-- ===========================================
-- USER ORGANIZATIONS (kobling mellom bruker og organisasjon)
-- ===========================================

CREATE TABLE IF NOT EXISTS user_organizations (
    id INT AUTO_INCREMENT PRIMARY KEY,
    user_id VARCHAR(255) NOT NULL,
    organization_id INT NOT NULL,

    CONSTRAINT uq_userorg_user UNIQUE (user_id),

    CONSTRAINT fk_userorg_org
        FOREIGN KEY (organization_id) REFERENCES organizations(id)
            ON DELETE CASCADE
            ON UPDATE CASCADE
);
