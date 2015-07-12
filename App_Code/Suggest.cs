using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using WebMatrix.Data;

public static class Suggestions
{
    public static bool ToggleEndorse(int suggestionID)
    {
        var user = UserHelper.GetUser();
        var db   = Database.Open(Website.DBName);

        try 
        { 
            db.Execute("INSERT INTO [Endorsements] VALUES (@0, @1)", user.UUN, suggestionID); 
            return true; 
        }
        catch 
        { 
            try 
            { 
                db.Execute("DELETE FROM [Endorsements] WHERE UUN=@0 AND Suggestion=@1", user.UUN, suggestionID); 
                return true; 
            } catch { return false; } 
        }
    }

    public static int Create(string title, bool isOrchestra, bool isChoir)
    {
        var user = UserHelper.GetUser();
        var db   = Database.Open(Website.DBName);

        var res = db.QueryValue("INSERT INTO Suggestions(Title,IsOrchestra,IsChoir,CreatorUUN) OUTPUT INSERTED.Suggestion VALUES (@0, @1, @2, @3)", 
            title.Length <= 120 ? title : title.Substring(0, 120), isOrchestra, isChoir, user.UUN);

        // Endorse your suggestion
        ToggleEndorse((int)res);
        return (int)res;
    }
    
    public static IEnumerable<dynamic> GetAll()
    {
        var db = Database.Open(Website.DBName);

        // Order by votes
        var res = db.Query(@"SELECT Suggestions.Suggestion, Title, IsChoir, IsOrchestra FROM Suggestions 
                             JOIN Endorsements ON Suggestions.Suggestion=Endorsements.Suggestion
                             GROUP BY Suggestions.Suggestion, Title, IsChoir, IsOrchestra
                             ORDER BY count(Suggestions.Suggestion) DESC");
        return res;
    }

    public static IEnumerable<int> GetEndorsed()
    {
        return GetEndorsed(UserHelper.GetUser().UUN);
    }
    public static IEnumerable<int> GetEndorsed(string userID)
    {
        var db = Database.Open(Website.DBName);

        var res = db.Query(@"SELECT DISTINCT Suggestions.Suggestion FROM Suggestions 
                             JOIN Endorsements ON Suggestions.Suggestion=Endorsements.Suggestion
                             WHERE UUN=@0", userID);
        
        // Convert to int array
        int[] output = new int[res.Count()];

        int i = 0;
        foreach (var entry in res) {
            output[i++] = entry.Suggestion;
        }
        return output;
    }
}
