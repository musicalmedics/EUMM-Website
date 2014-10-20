﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

/// <summary>
/// Summary description for Locale
/// </summary>
public static class Locale
{
    static string name;

    static Dictionary<string, Dictionary<string, string>>
        messages = new Dictionary<string, Dictionary<string, string>>();

    static Locale()
    {
        messages.Add("en",
            new Dictionary<string, string>()
            {
                {"ErrorUUN",     "You must specify a valid student number (UUN.)"},
                {"ErrorNoPass",  "You must specify a password."},
                {"ErrorPassLen", "Password must be at least 6 characters."},
                {"ErrorLogin",   "The student number or password provided is incorrect."},
                {"ErrorValid",   "Log in was unsuccessful. Please correct the errors and try again."},
                {"PromptLogin",  "Members, use your student no. to log in."},
                {"Original",     "Original"},
                {"PaperCopy",    "Paper Copy"},
                {"Digital",      "Digital"},
                {"Other",        "Other"},
                {"Preparing",    "Preparing"},
                {"Cancelled",    "Cancelled"},
                {"Loaned",       "Loaned"},
                {"Returned",     "Returned"},
                {"LatewithFee",  "Late with Fee"},
                {"FeesDue",      "FeesDue"},
                {"FeesPaid",     "Fees Paid"},
                {"Overdue",      "Overdue"}
            });
        Current = messages.First().Key;
    }

    public static string Current
    {
        get { return name; }
        set
        {
            Messages = messages[value];
            name     = value;
        }
    }

    public static Dictionary<string, string> Messages { get; private set; }
}