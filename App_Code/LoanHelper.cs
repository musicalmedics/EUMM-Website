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
        var db = Database.Open(Website.DBName);
        return db.Query("SELECT * FROM Loans WHERE Returned='0' AND Part='" + partID + "'");
    }

    public static bool IsRenting(IEnumerable<dynamic> loans)
    {
        return loans.Where((l) => l.Member == WebSecurity.CurrentUserName).Any();
    }

    public static IEnumerable<dynamic> GetParts(string pieceID, string orderBy="Instrument")
    {
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

    public static int CreateLoan(string partID, string format, DateTime returnDate)
    {
        return CreateLoan(WebSecurity.CurrentUserName, partID, format, returnDate);
    }
    public static int CreateLoan(string member, string partID, string format, DateTime returnDate)
    {
        DateTime ret = returnDate;
        Database db  = Database.Open(Website.DBName);

        // Digital copies are automatically fulfilled
        char fulfilled = (format == Website.Format_DigitalCopy.ToString()) ? '1' : '0';

        return db.Execute(String.Format
        (
            "INSERT INTO Loans (Part,Member,Format,LoanStart,LoanEnd,Fulfilled) " +
                    "VALUES ('{0}','{1}','{2}',GETDATE(),'{3}-{4}-{5}','{6}')",

                    partID, member, format, ret.Year, ret.Month, ret.Day, fulfilled)
        );
    }
}