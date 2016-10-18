using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using WebMatrix.Data;
using WebMatrix.WebData;

/// <summary>
/// Summary description for LoanHelper
/// </summary>
public static class LoanHelper
{
    public static IEnumerable<dynamic> GetInstrumentList()
    {
        return Database.Open(Website.DBName).Query("SELECT DISTINCT Instrument FROM Parts WHERE Parts.IsPresent='1'");
    }

    public static IEnumerable<dynamic> GetPieces()
    {
        var db = Database.Open(Website.DBName);
        var q = db.Query("SELECT * FROM Pieces WHERE IsPresent='1' ORDER BY NextPerformance DESC, Title");

        dynamic user = UserHelper.GetUser();

        // Filter pieces by whether the user is a member of that library
        return q.Where((p) => (Website.IsOrchestra(p.Libraries) && user.IsOrchestra)
            || (Website.IsChoir(p.Libraries) && user.IsChoir));
    }
    public static IEnumerable<dynamic> GetLoanablePieces()
    {
        var db = Database.Open(Website.DBName);
        var q  = db.Query("SELECT * FROM Pieces WHERE IsPresent='1' AND CanLendParts='1' ORDER BY Title");

        dynamic user = UserHelper.GetUser();

        // Filter pieces by whether the user is a member of that library
        return q.Where((p) => (Website.IsOrchestra(p.Libraries) && user.IsOrchestra)
            || (Website.IsChoir(p.Libraries) && user.IsChoir));
    }

    public static IEnumerable<dynamic> GetCurrentLoans(string partID)
    {
        int.Parse(partID); // Validate input

        var db = Database.Open(Website.DBName);
        return db.Query("SELECT * FROM Loans WHERE Returned='0' AND Part=@0", partID);
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
            return db.Query("SELECT * FROM [Loans - Simple] WHERE [Libraries] & @0 != 0 ORDER BY [LoanStart] DESC", Website.Library_Orchestra);
        }
        else if (user.IsChoir) {
            return db.Query("SELECT * FROM [Loans - Simple] WHERE [Libraries] & @0 != 0 ORDER BY [LoanStart] DESC", Website.Library_Choir);
        }
        // Return empty set
        return db.Query("SELECT * FROM [Loans - Simple] WHERE 1 = 0");
    }

    public static bool IsRenting(IEnumerable<dynamic> loans)
    {
        return loans.Where((l) => l.Member == WebSecurity.CurrentUserName).Any();
    }

    public static IEnumerable<dynamic> GetParts(string pieceID, string orderBy="Instrument")
    {
        int.Parse(pieceID); // Validate input

        var db = Database.Open(Website.DBName);
        var q  = db.Query("SELECT * FROM Parts WHERE Piece=@0 AND IsPresent='1' ORDER BY "+orderBy, pieceID);

        dynamic Parts = Website.ExpandoFromTable(q);

        foreach (var part in Parts) {
            part.Piece = db.QuerySingle("SELECT * FROM Pieces WHERE ID=@0", part.Piece);
        }
        return Parts;
    }

    public static bool CanLoan(dynamic part)
    {
        var loans = GetCurrentLoans(part.ID.ToString());

        int libraries = (UserHelper.GetUser().IsOrchestra ? Website.Library_Orchestra : 0) |
            (UserHelper.GetUser().IsChoir ? Website.Library_Choir : 0);

        // Check if user's available libraries contain the part
        if ((libraries & part.Piece.Libraries) == 0) {
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

        var tokens = db.Query("SELECT * FROM DlTokens WHERE Part=@0 AND Member=@1",
            partID, WebSecurity.CurrentUserName);

        if (!tokens.Any()) {
            throw new Exception("No valid download token for this part");
        }
        var token = tokens.First();
        db.Execute("DELETE FROM DlTokens WHERE Token=@0", token.Token);

        return HttpContext.Current.Server.MapPath(System.IO.Path.Combine(Website.HiddenDir, 
            (string)db.QueryValue("SELECT DigitalCopyPath FROM Parts WHERE ID=@0", partID)));
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
            int i = db.Execute("INSERT INTO DlTokens (Member, Part) VALUES (@0, @1)", member, partID);
        }

        return db.Execute(String.Format
        (
            @"INSERT INTO Loans (Part,Member,Format,LoanStart,LoanEnd,Fulfilled)
                    VALUES (@0,@1,@2,GETDATE(),'{0}-{1}-{2}',@3)", ret.Year, ret.Month, ret.Day), 

            partID, member, format, fulfilled
        );
    }

    public static int CancelRequest(int requestID)
    {
        return Database.Open(Website.DBName)
            .Execute("DELETE FROM Loans WHERE ID=@0 AND Fulfilled='0'", requestID);
    }

    public static int AddPiece(string title, string author, int library, bool owned, 
        string arranger, string opus, string edition, DateTime? loanEnd, DateTime? performance,
        string notes)
    {
        // Pre-format dates because Database.Execute is pretty bad
        string dloan = null, dperf = null;

        if (loanEnd != null) {
            dloan = String.Format("'{0}-{1}-{2}'", loanEnd.Value.Year, loanEnd.Value.Month, loanEnd.Value.Day);
        }
        else if (performance != null) {
            dperf = String.Format("'{0}-{1}-{2}'", performance.Value.Year, performance.Value.Month,
                performance.Value.Day);
        }

        if (String.IsNullOrWhiteSpace(arranger)) arranger = null;
        if (String.IsNullOrWhiteSpace(opus))     opus     = null;
        if (String.IsNullOrWhiteSpace(edition))  edition  = null;
        if (String.IsNullOrWhiteSpace(notes))    notes    = null;

        return (int)Database.Open(Website.DBName).QueryValue("INSERT INTO Pieces "+
            "(Title,Author,ArrangedBy,Opus,Edition,Libraries,IsPresent,IsOwned,LoanEnd,NextPerformance,NotesUponLoan) "+
            "OUTPUT INSERTED.ID "+
            "VALUES (@0,@1,@2,@3,@4,@5,1,@6,@7,@8,@9)",
                
            title, author, arranger, opus, edition, library.ToString(), (owned?"1":"0"), dloan, dperf, notes);
    }

    public static int SetPieceFlags(string id, bool loanable, bool present)
    {
        return Database.Open(Website.DBName).Execute("UPDATE Pieces SET CanLendParts=@0, IsPresent=@1 WHERE ID=@2",
            (loanable ? "1" : "0"), (present ? "1" : "0"), id);
    }

    public static int EditPart(string id, bool canLendOrig, bool canLendCopy, bool canLendDigital, 
        float deposit)
    {
        return Database.Open(Website.DBName).Execute("UPDATE Parts SET CanLendOriginal=@0, " +
            "CanLendCopy=@1, CanLendDigital=@2, Deposit=@3 WHERE ID=@4",

            (canLendOrig ? "1" : "0"), (canLendCopy ? "1" : "0"), (canLendDigital ? "1" : "0"), deposit, id);
    }

    public static int HidePart(string partID)
    {
        return Database.Open(Website.DBName).Execute("UPDATE Parts SET IsPresent='0' WHERE ID=@0", partID);
    }

    public static string GetPartFilePath(string partID)
    {
        var db = Database.Open(Website.DBName);

        return db.QueryValue("SELECT DigitalCopyPath FROM Parts WHERE ID=@0", partID) as string;
    }

    public static bool ReplacePartFile(string partID, string filepath)
    {
        var db = Database.Open(Website.DBName);

        // Get previous filename
        string previous = db.QueryValue(
            "SELECT DigitalCopyPath FROM Parts WHERE ID=@0", partID) as string;

        // Change filename
        var count = db.Execute("UPDATE Parts SET DigitalCopyPath=@0 WHERE ID=@1", filepath, partID);
        if (count == 0) return false;

        // Now remove old file (if not the same as new one)
        if (!String.IsNullOrWhiteSpace(previous) && 
                (String.IsNullOrWhiteSpace(filepath) ||  
                    System.IO.Path.GetFullPath(previous) != System.IO.Path.GetFullPath(filepath)))
        {
            return Website.DeleteHiddenFile(previous);
        }
        return true;
    }

    public static int AddPart(int pieceID, string instrument, string designation, int? count, bool original,
        bool paper, bool digital, string filepath, float deposit)
    {
        if (String.IsNullOrWhiteSpace(instrument)) instrument = null;
        if (String.IsNullOrWhiteSpace(designation)) designation = null;


        return Database.Open(Website.DBName).Execute("INSERT INTO Parts " +
            "(Piece,Instrument,Designation,Count,CanLendOriginal,CanLendCopy,CanLendDigital,DigitalCopyPath,Deposit) " +
            "VALUES (@0,@1,@2,@3,@4,@5,@6,@7,@8)",

            pieceID, instrument, designation, count, original ? "1" : "0", paper ? "1" : "0", digital ? "1" : "0",
            filepath, deposit);
    }
}
