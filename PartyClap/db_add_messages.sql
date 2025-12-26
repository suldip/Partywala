USE PartyClapDB;

CREATE TABLE IF NOT EXISTS Messages (
    Id VARCHAR(50) PRIMARY KEY,
    SenderId VARCHAR(50) NOT NULL,
    ReceiverId VARCHAR(50) NOT NULL,
    SenderRole VARCHAR(20) NOT NULL,
    Content TEXT NOT NULL,
    Timestamp DATETIME DEFAULT CURRENT_TIMESTAMP,
    IsRead BOOLEAN DEFAULT FALSE
);

DROP PROCEDURE IF EXISTS sp_SendMessage;
DROP PROCEDURE IF EXISTS sp_GetChatHistory;
DROP PROCEDURE IF EXISTS sp_GetUnreadCount;

DELIMITER //

CREATE PROCEDURE sp_SendMessage(
    IN p_Id VARCHAR(50),
    IN p_SenderId VARCHAR(50),
    IN p_ReceiverId VARCHAR(50),
    IN p_SenderRole VARCHAR(20),
    IN p_Content TEXT
)
BEGIN
    INSERT INTO Messages (Id, SenderId, ReceiverId, SenderRole, Content, Timestamp, IsRead)
    VALUES (p_Id, p_SenderId, p_ReceiverId, p_SenderRole, p_Content, NOW(), FALSE);
END //

CREATE PROCEDURE sp_GetChatHistory(
    IN p_User1Id VARCHAR(50),
    IN p_User2Id VARCHAR(50)
)
BEGIN
    SELECT * FROM Messages 
    WHERE (SenderId = p_User1Id AND ReceiverId = p_User2Id)
       OR (SenderId = p_User2Id AND ReceiverId = p_User1Id)
    ORDER BY Timestamp ASC;
END //

CREATE PROCEDURE sp_GetUnreadCount(
    IN p_UserId VARCHAR(50)
)
BEGIN
    SELECT COUNT(*) FROM Messages WHERE ReceiverId = p_UserId AND IsRead = FALSE;
END //

DELIMITER ;
