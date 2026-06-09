-- Mumbai / Thane / Navi Mumbai / Raigad locations for PartyClap
USE partyclapdb;

INSERT INTO Locations (PinCode, AreaName, City, State) VALUES
('400001', 'Fort', 'Mumbai', 'Maharashtra'),
('400002', 'Kalbadevi', 'Mumbai', 'Maharashtra'),
('400014', 'Dadar East', 'Mumbai', 'Maharashtra'),
('400028', 'Dadar West', 'Mumbai', 'Maharashtra'),
('400050', 'Bandra West', 'Mumbai', 'Maharashtra'),
('400051', 'Bandra East', 'Mumbai', 'Maharashtra'),
('400058', 'Andheri West', 'Mumbai', 'Maharashtra'),
('400069', 'Andheri East', 'Mumbai', 'Maharashtra'),
('400071', 'Chembur', 'Mumbai', 'Maharashtra'),
('400077', 'Ghatkopar East', 'Mumbai', 'Maharashtra'),
('400080', 'Mulund West', 'Mumbai', 'Maharashtra'),
('400081', 'Mulund East', 'Mumbai', 'Maharashtra'),
('400601', 'Thane West', 'Thane', 'Maharashtra'),
('400602', 'Thane East', 'Thane', 'Maharashtra'),
('400703', 'Vashi', 'Navi Mumbai', 'Maharashtra'),
('400706', 'Nerul', 'Navi Mumbai', 'Maharashtra'),
('400614', 'CBD Belapur', 'Navi Mumbai', 'Maharashtra'),
('410206', 'Panvel', 'Raigad', 'Maharashtra'),
('421201', 'Dombivli East', 'Thane', 'Maharashtra'),
('421301', 'Kalyan West', 'Thane', 'Maharashtra')
ON DUPLICATE KEY UPDATE
    AreaName = VALUES(AreaName),
    City = VALUES(City),
    State = VALUES(State);

-- Ensure Maharashtra is enabled for vendor/customer area selection
UPDATE States SET IsEnabled = 1 WHERE Name = 'Maharashtra';
