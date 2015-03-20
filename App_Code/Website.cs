/* Website.cs - (c) James S Renwick 2014
 * -------------------------------------
 * Version 1.2.0
 */
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.SessionState;


/// <summary>
/// Class containing website-wide helper methods and constants.
/// </summary>
public static partial class Website
{
    /// <summary>
    /// Use to conditionally show or hide an HTML element.
    /// </summary>
    public static string ShowIf(bool cond)
    {
        if (!cond) return "style=visibility:hidden";
        else       return "style=visibility:visible";
    }

    public static bool IsChoir(int libraryValue)
    {
        return (libraryValue & Website.Library_Choir) != 0;
    }
    public static bool IsOrchestra(int libraryValue)
    {
        return (libraryValue & Website.Library_Orchestra) != 0;
    }

    /// <summary>
    /// Gets a formatted currency string for the given double value.
    /// </summary>
    public static string GetPounds(double f)
    {
        return f.ToString("C2", CultureInfo.CreateSpecificCulture("en-GB"));
    }

    public static HttpSessionState Session
    {
        get { return HttpContext.Current.Session; }
    }

    /// <summary>
    /// Sanitises the input string by replacing symbols and control chars 
    /// with underscores. If input is null, returns null.
    /// </summary>
    public static string Sanitise(string value)
    {
        if (value == null) return null;

        var sb = new System.Text.StringBuilder(value.Length);
        foreach (char c in value)
        {
            if      (c == '@')                              sb.Append(c);
            else if (char.IsControl(c) || char.IsSymbol(c)) sb.Append('_');
            else                                            sb.Append(c);
        }
        return sb.ToString();
    }

    /// <summary>
    /// Gets the return date for the loan.
    /// 
    /// If both are defined, takes the closest date from
    /// the next performance and the return-to-uni-library date.
    /// 
    /// If one of those is not null, uses that. Otherwise,
    /// defaults to a loan period of one month.
    /// </summary>
    /// <param name="piece">The piece record for which to get the return date.</param>
    public static DateTime CalculateReturnDate(dynamic piece)
    {
        DateTime? end = piece.LoanEnd;
        DateTime? per = piece.NextPerformance;
        DateTime  def = DateTime.Now.AddMonths(1); // Fallback is 1 month

        if (end != null && per != null) {
            return end.Value < per.Value ? end.Value : per.Value;
        }
        else return (DateTime)(end ?? per ?? def);
    }

    /// <summary>
    /// Converts the row object into a dynamic object whose
    /// fields and values you can edit.
    /// </summary>
    public static dynamic ExpandoFromRow(dynamic row)
    {
        IDictionary<string, object> obj = new ExpandoObject();

        foreach (var column in row.Columns) {
            obj.Add(column, row[column]);
        }
        return (ExpandoObject)obj;
    }
    /// <summary>
    /// Converts the given table into a dynamic object whose
    /// fields and values you can edit.
    /// </summary>
    public static dynamic[] ExpandoFromTable(IEnumerable<dynamic> table)
    {
        ExpandoObject[] output = new ExpandoObject[table.Count()];

        int i = 0;
        foreach (var row in table)
        { 
            IDictionary<string, object> obj = new ExpandoObject();

            foreach (var column in row.Columns) {
                obj.Add(column, row[column]);
            }
            output[i++] = (ExpandoObject)obj;
        }
        return output;
    }

    public static void RedirectToDownload(string filepath, string contentType, HttpResponseBase response)
    {
        FileInfo file = new FileInfo(filepath);
        if (file.Exists)
        {
            response.ClearContent();
            response.AddHeader("Content-Disposition", "attachment; filename=" + file.Name);
            response.AddHeader("Content-Length", file.Length.ToString());
            response.ContentType = contentType;
            response.TransmitFile(file.FullName);
            response.End();
        }
        else throw new FileNotFoundException();
    }
}