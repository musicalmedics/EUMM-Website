using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Web;
using WebMatrix.Data;

public static class Suggestions
{
    public static bool ToggleEndorse(int suggestionID)
    {
        var db    = Database.Open(Website.DBName);
        var user  = UserHelper.GetUser();
        int ipRaw = Website.ClientIntegerIP;

        if (UserHelper.IsGuest(user.UUN))
        {
            // Check if existing for this IP as we're guest
            int existing = db.QueryValue("SELECT COUNT(*) FROM [Endorsements2] WHERE UUN=@0 AND IP=@1 AND Suggestion=@2", 
                                            user.UUN, ipRaw, suggestionID);
            // Add or delete to toggle
            if (existing != 0) {
                db.Execute("DELETE FROM [Endorsements2] WHERE UUN=@0 AND IP=@1 AND Suggestion=@2", user.UUN, ipRaw, suggestionID);
            }
            else db.Execute("INSERT INTO [Endorsements2] (IP, Suggestion) VALUES (@0, @1)", ipRaw, suggestionID);
        }
        else
        {
            // Check if existing for this user
            int existing = db.QueryValue("SELECT COUNT(*) FROM [Endorsements2] WHERE UUN=@0 AND Suggestion=@1",
                                            user.UUN, suggestionID);
            // Add or delete to toggle
            if (existing != 0) {
                db.Execute("DELETE FROM [Endorsements2] WHERE UUN=@0 AND Suggestion=@1", user.UUN, suggestionID);
            }
            else db.Execute("INSERT INTO [Endorsements2] (UUN, Suggestion) VALUES (@0, @1)", user.UUN, suggestionID);
        }
        return true; 
        
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
                             JOIN Endorsements2 ON Suggestions.Suggestion=Endorsements2.Suggestion
                             WHERE Suggestions.Active='1'
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
        var db    = Database.Open(Website.DBName);
        int ipRaw = Website.ClientIntegerIP;

        IEnumerable<dynamic> res;

        if (UserHelper.IsGuest(userID))
        {
            res = db.Query(@"SELECT DISTINCT Suggestions.Suggestion FROM Suggestions 
                             JOIN Endorsements2 ON Suggestions.Suggestion=Endorsements2.Suggestion
                             WHERE UUN=@0 AND Endorsements2.IP=@1", userID, ipRaw);
        }
        else
        {
            res = db.Query(@"SELECT DISTINCT Suggestions.Suggestion FROM Suggestions 
                             JOIN Endorsements2 ON Suggestions.Suggestion=Endorsements2.Suggestion
                             WHERE UUN=@0", userID);
        }
        
        // Convert to int array
        int[] output = new int[res.Count()];

        int i = 0;
        foreach (var entry in res) {
            output[i++] = entry.Suggestion;
        }
        return output;
    }
}
