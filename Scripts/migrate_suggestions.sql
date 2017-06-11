DELETE FROM Suggestions2;

INSERT INTO Suggestions2
SELECT S1.Suggestion, S1.Title, CASE WHEN S1.IsChoir='1' THEN '2' ELSE '1' END AS [Group], S1.CreatorUUN, S1.Active
FROM Suggestions S1;
