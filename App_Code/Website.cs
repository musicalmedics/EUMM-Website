/* Website.cs - (c) James S Renwick 2014
 * -------------------------------------
 * Version 1.2.0
 */
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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

    public static string SanitiseFilename(string filename)
    {
        if (filename == null) return null;

        var sb = new System.Text.StringBuilder(filename.Length);

        var badchars = System.IO.Path.GetInvalidFileNameChars();

        foreach (char c in filename) {
            sb.Append(badchars.Contains(c) ? '_' : c);
        }
        return sb.ToString();
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


    public static int ClientIntegerIP
    {
        get
        {
            int ipRaw = 0;
            IPAddress address;

            // Get current IP address as int, or 0 if unavailable
            if (IPAddress.TryParse(HttpContext.Current.Request.UserHostAddress, out address) &&
                address.AddressFamily == AddressFamily.InterNetwork)
            {
#pragma warning disable CS0618 // Only used with IPv4 connections, so okay
                ipRaw = (int)(uint)address.Address;
#pragma warning restore CS0618
            }
            return ipRaw;
        }
    }

}
