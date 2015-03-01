using System;
using System.Collections.Generic;
using System.Linq;
using WebMatrix.Data;
using WebMatrix.WebData;

/// <summary>
/// Summary description for LoanHelper
/// </summary>
public static class LoanHelper
{
    public static IEnumerable<dynamic> GetInstrumentList()
    {
        return Database.Open(Website.DBName).Query("SELECT DISTINCT Instrument FROM Parts");
    }

    public static IEnumerable<dynamic> GetLoanablePieces()
    {
        var db = Database.Open(Website.DBName);
        var q  = db.Query("SELECT * FROM Pieces WHERE IsPresent='1' AND CanLendParts='1'");

        dynamic user = UserHelper.GetUser();

        // Filter pieces by whether the user is a member of that library
        return q.Where((p) => (p.Library == 0 && user.IsOrchestra) 
            || (p.Library == 1 && user.IsChoir));
    }

    public static IEnumerable<dynamic> GetCurrentLoans(string partID)
    {
        int.Parse(partID); // Validate input

        var db = Database.Open(Website.DBName);
        return db.Query("SELECT * FROM Loans WHERE Returned='0' AND Part='" + partID + "'");
    }

    public static IEnumerable<dynamic> GetAllCurrentLoans()
    {
        var user = UserHelper.GetUser();

        if (!user.IsAdmin) throw new Exception("Administrative privileges requried for this operation");
        if (!user.IsOrchestra && !user.IsChoir) return new List<Object>();

        var db = Database.Open(Website.DBName);

        if (user.IsOrchestra && user.IsChoir) {
            return db.Query("SELECT * FROM [Loans - Simple] ORDER BY [LoanStart] DESC");
        }
        else if (user.IsOrchestra) {
            return db.Query("SELECT * FROM [Loans - Simple] WHERE [Library]='0' ORDER BY [LoanStart] DESC");
        }
        else /*if (user.IsChoir) */ {
            return db.Query("SELECT * FROM [Loans - Simple] WHERE [Library]='1' ORDER BY [LoanStart] DESC");
        }
    }

    public static bool IsRenting(IEnumerable<dynamic> loans)
    {
        return loans.Where((l) => l.Member == WebSecurity.CurrentUserName).Any();
    }

    public static IEnumerable<dynamic> GetParts(string pieceID, string orderBy="Instrument")
    {
        int.Parse(pieceID); // Validate input

        var db = Database.Open(Website.DBName);
        var q  = db.Query("SELECT * FROM Parts WHERE Piece='" + pieceID + "' ORDER BY "+orderBy);

        dynamic Parts = Website.ExpandoFromTable(q);

        foreach (var part in Parts) {
            part.Piece = db.QuerySingle("SELECT * FROM Pieces WHERE ID='" + part.Piece + "'");
        }
        return Parts;
    }

    public static bool CanLoan(dynamic part)
    {
        var loans = GetCurrentLoans(part.ID.ToString());

        // Check if the user is actually a member of the piece's library
        if (part.Piece.Library == Website.Library_Orchestra 
            && !UserHelper.GetUser().IsOrchestra) {
            return false;
        }
        if (part.Piece.Library == Website.Library_Choir
            && !UserHelper.GetUser().IsChoir) {
            return false;
        }

        // Check if the user already has the part on loan and it's not public domain
        if (part.Count != null && IsRenting(loans)) return false;

        // Check if there are any copies left (null indicates infinite)
        return part.Count == null || (part.Count - loans.Count) > 0;
    }

    public static string GetDownloadPath(string partID)
    {
        int.Parse(partID); // Validate input

        Database db = Database.Open(Website.DBName);

        var tokens = db.Query(String.Format("SELECT * FROM DlTokens WHERE Part='{0}' AND Member='{1}'",
            partID, WebSecurity.CurrentUserName));

        if (!tokens.Any()) {
            throw new Exception("No valid download token for this part");
        }
        var token = tokens.First();
        db.Execute("DELETE FROM DlTokens WHERE Token='"+token.Token+"'");

        return db.QueryValue("SELECT DigitalCopyPath FROM Parts WHERE ID='"+partID+"'");
    }

    public static int CreateLoan(string partID, string format, DateTime returnDate)
    {
        return CreateLoan(WebSecurity.CurrentUserName, partID, format, returnDate);
    }
    public static int CreateLoan(string member, string partID, string format, DateTime returnDate)
    {
        // Validate input
        int.Parse(partID); int.Parse(format); member = Website.Sanitise(member);

        DateTime ret = returnDate;
        Database db  = Database.Open(Website.DBName);

        char fulfilled = '0';

        if (format == Website.Format_DigitalCopy.ToString())
        {
            fulfilled = '1'; // Digital copies are automatically fulfilled

            // Add digital download token for that type and user
            int i = db.Execute(String.Format("INSERT INTO DlTokens (Member, Part) VALUES ('{0}','{1}')",
                member, partID));
        }

        return db.Execute(String.Format
        (
            "INSERT INTO Loans (Part,Member,Format,LoanStart,LoanEnd,Fulfilled) " +
                    "VALUES ('{0}','{1}','{2}',GETDATE(),'{3}-{4}-{5}','{6}')",

                    partID, member, format, ret.Year, ret.Month, ret.Day, fulfilled)
        );
    }

    public static int CancelRequest(int requestID)
    {
        return Database.Open(Website.DBName).Execute(
            String.Format("DELETE FROM Loans WHERE ID='{0}' AND Fulfilled='0'", requestID.ToString()));
    }
}