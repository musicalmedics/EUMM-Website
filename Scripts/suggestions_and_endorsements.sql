SELECT Title, COUNT(*) FROM dbo.Suggestions JOIN dbo.Endorsements2 ON dbo.Suggestions.Suggestion=dbo.Endorsements2.Suggestion 
GROUP BY dbo.Suggestions.Suggestion, dbo.Suggestions.Title ORDER BY COUNT(*) DESC;

SELECT Title, FirstName, LastName FROM dbo.Endorsements2 JOIN dbo.Members ON dbo.Endorsements2.UUN=dbo.Members.UUN
														 JOIN dbo.Suggestions ON dbo.Suggestions.Suggestion=dbo.Endorsements2.Suggestion;
