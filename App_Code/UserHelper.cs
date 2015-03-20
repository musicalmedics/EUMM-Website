﻿using System;
using System.Linq;
using System.Collections.Specialized;
using System.Collections.Generic;
using WebMatrix.Data;
using WebMatrix.WebData;
using System.Web.Security;
using System.Web;

/// <summary>
/// Summary description for UserHelper
/// </summary>
public static class UserHelper
{
    public static bool IsMember()
    {
        return IsMember(WebSecurity.CurrentUserName);
    }
    public static bool IsMember(string studentID)
    {
        // Open the database & look for a member
        var db  = Database.Open(Website.DBName);
        var res = db.Query("SELECT * FROM Members WHERE UUN=@0", studentID);

        int count = res.Count();
        if (count == 0) return false;
        if (count == 1) return (bool)(res.First().IsMember);

        else throw new Exception("More than one member matches the given UUN");
    }

    public static dynamic GetMembers(bool showInactive=false)
    {
        var db = Database.Open(Website.DBName);
        return db.Query("SELECT * from Members" + (!showInactive ? " WHERE IsMember='1'":""));
    }

    public static bool IsAdmin()
    {
        return IsAdmin(WebSecurity.CurrentUserName);
    }
    public static bool IsAdmin(string studentID)
    {
        // Open the database & look for the member
        var db  = Database.Open(Website.DBName);
        var res = db.Query("SELECT * FROM Members WHERE UUN=@0", studentID);

        int count = res.Count();
        if (count == 0) return false;
        if (count == 1) return (bool)(res.First().IsAdmin);

        else throw new Exception("More than one member matches the given UUN");
    }

    public static dynamic GetUser()
    {
        // Open the database & look for the user
        var db = Database.Open(Website.DBName);

        return db.QuerySingle("SELECT * FROM Members WHERE UUN=@0", WebSecurity.CurrentUserName);
    }
    public static dynamic GetUser(string studentID)
    {
        // Open the database & look for the user
        var db  = Database.Open(Website.DBName);
        return db.QuerySingle("SELECT * FROM Members WHERE UUN=@0", studentID);
    }

    public static int ConfirmUser(string studentID, NameValueCollection form)
    {
        // Open the database & update the user
        var db = Database.Open(Website.DBName);

        string firstName = Website.Sanitise(form["firstName"]);
        string lastName  = Website.Sanitise(form["lastName"]);

        // Trim strings to match db requirement
        if (firstName.Length > 50) firstName = firstName.Remove(50);
        if (lastName.Length > 50)  lastName  = lastName.Remove(50);

        // Update member record
        return db.Execute("UPDATE Members SET FirstName=@0,LastName=@1,Email=@2 WHERE UUN=@3",
            firstName, lastName, form["email"], studentID);
    }

    public static int CountRequests()
    {
        var user = GetUser();

        if (!user.IsAdmin) throw new Exception("Administrative privileges requried for this operation");
        if (!user.IsOrchestra && !user.IsChoir) return 0;

        var db = Database.Open(Website.DBName);

        if (user.IsOrchestra && user.IsChoir) {
            return db.QueryValue("SELECT COUNT(*) FROM [New Loan Requests - Simple]");
        }
        else if (user.IsOrchestra) {
            return db.QueryValue("SELECT COUNT(*) FROM [New Loan Requests - Simple] WHERE [Libraries] & @0 != 0", Website.Library_Orchestra);
        }
        else if (user.IsChoir) {
            return db.QueryValue("SELECT COUNT(*) FROM [New Loan Requests - Simple] WHERE [Libraries] & @0 != 0", Website.Library_Choir);
        }
        else return 0;
    }

    public static IEnumerable<dynamic> GetRequests()
    {
        var user = GetUser();

        if (!user.IsAdmin) throw new Exception("Administrative privileges requried for this operation");
        if (!user.IsOrchestra && !user.IsChoir) return new List<Object>();

        var db = Database.Open(Website.DBName);

        if (user.IsOrchestra && user.IsChoir) {
            return db.Query("SELECT * FROM [New Loan Requests - Simple]");
        }
        else if (user.IsOrchestra) {
            return db.Query("SELECT * FROM [New Loan Requests - Simple] WHERE [Libraries] & @0 != 0", Website.Library_Orchestra);
        }
        else if (user.IsChoir) {
            return db.Query("SELECT * FROM [New Loan Requests - Simple] WHERE [Libraries] & @0 != 0", Website.Library_Choir);
        }
        // Return empty set
        else return db.Query("SELECT * FROM [New Loan Requests - Simple] WHERE 1 = 0");
    }

    public static dynamic GetCurrentLoans(string studentID)
    {
        // Open the database & update the user
        var db = Database.Open(Website.DBName);

        // Perform the query
        var q = db.Query("SELECT * FROM Loans WHERE Member=@0 AND Returned='0'", studentID);

        var Loans = Website.ExpandoFromTable(q);

        // Query for relations
        foreach (var row in Loans)
        {
            row.Part = Website.ExpandoFromRow(db.QuerySingle("SELECT * FROM Parts WHERE ID=@0", row.Part));
            row.Part.Piece = db.QuerySingle("SELECT * FROM Pieces WHERE ID=@0", row.Part.Piece);
        }
        return Loans;
    }    

    public static dynamic GetPreviousLoans(string studentID)
    {
        // Open the database & update the user
        var db = Database.Open(Website.DBName);

        // Perform the query
        var q = db.Query("SELECT * FROM Loans WHERE Member=@0 AND Returned='1'", studentID);

        var Loans = Website.ExpandoFromTable(q);

        // Query for relations
        foreach (var row in Loans)
        {
            row.Part = Website.ExpandoFromRow(db.QuerySingle("SELECT * FROM Parts WHERE ID=@0", row.Part));
            row.Part.Piece = db.QuerySingle("SELECT * FROM Pieces WHERE ID=@0", row.Part.Piece);
        }
        return Loans;
    }

    public static int AddMember(string uun, string fname, string lname, string email,
                bool member, bool orchestra, bool choir, bool admin)
    {
        if (email != null) email = email.Replace(" ","");
        if (String.Empty == email) email = null;

        var db = Database.Open(Website.DBName);

        return db.Execute(String.Format(
            @"INSERT INTO Members (UUN, FirstName, LastName, Email, IsMember, 
            JoinDate, IsOrchestra, IsChoir, IsAdmin) VALUES (@0,@1,@2,@3,@4,GETDATE(),@5,@6,@7)",
            
            uun, fname, lname, email, (member?"1":"0"),(orchestra?"1":"0"),(choir?"1":"0"),(admin?"1":"0")
        ));
    }

    ///<summary>Easter Egg!</summary>
    public static bool IsSmashing
    {
        get {
            return (WebSecurity.CurrentUserName == "s1311545" && (string)Website.Session["smash"] != "False") 
                || (string)Website.Session["smash"] == "True"; 
        }
        set { Website.Session["smash"] = value.ToString(); }
    }
}