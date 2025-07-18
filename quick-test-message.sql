-- Test için bir mesajın çalışma zamanını şimdiki zamana güncelle
UPDATE ScheduleMessages 
SET NextRunTime = GETUTCDATE()
WHERE Id = 'aeca8287-93da-4e96-aeed-08ddc5e67019';
 
-- Kontrol için
SELECT Id, Title, MessageContent, NextRunTime, Status, IsActive 
FROM ScheduleMessages 
WHERE Id = 'aeca8287-93da-4e96-aeed-08ddc5e67019'; 