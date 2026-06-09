-- B045: Admin-configurable platform fee (and other app settings)
-- Run once on production MySQL if the table was not auto-created at startup.

CREATE TABLE IF NOT EXISTS AppSettings (
    SettingKey VARCHAR(64) PRIMARY KEY,
    SettingValue VARCHAR(255) NULL,
    UpdatedDate DATETIME DEFAULT CURRENT_TIMESTAMP
);

INSERT IGNORE INTO AppSettings (SettingKey, SettingValue) VALUES ('PlatformFeePercent', '10');

SELECT * FROM AppSettings WHERE SettingKey = 'PlatformFeePercent';
