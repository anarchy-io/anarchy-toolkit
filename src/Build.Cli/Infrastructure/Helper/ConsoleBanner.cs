namespace Anarchy.Toolkit.Build.Cli.Infrastructure.Helper;

/// <summary>
/// Anarchy's logo, as printed by <c>scripts/console/banner.sh</c>: the wide wordmark on terminals of at least
/// <see cref="WideColumns"/> columns, otherwise the compact emblem.
/// </summary>
public static class ConsoleBanner
{
    public const int WideColumns = 116;

    private const string Red = "\e[0;31m";
    private const string Yellow = "\e[0;33m";
    private const string Reset = "\e[0m";

    // {R} and {Y} switch to red and yellow, as the CL_RED and CL_YELLOW variables do in the scripts.
    private static readonly string[] Wide = [
        "{R}",
        "",
        "",
        "   ▄████████ ███▄▄▄▄      ▄████████    ▄████████  ▄████████    ▄█    █▄    ▄██   ▄  {Y}    ▀█████▀      ▄██▀ ▄██████▄ {R}",
        "  ███    ███ ███▀▀▀██▄   ███    ███   ███    ███ ███    ███   ███    ███   ███   ██▄{Y}      ███       ▄██▀ ███    ███{R}",
        "  ███    ███ ███   ███   ███    ███   ███    ███ ███    █▀    ███    ███   ███▄▄▄███{Y}      ███      ▄██▀  ███    ███{R}",
        "  ███    ███ ███   ███   ███    ███  ▄███▄▄▄▄██▀ ███         ▄███▄▄▄▄███▄▄ ▀▀▀▀▀▀███{Y}      ███      ███   ███    ███{R}",
        "▀███████████ ███   ███ ▀███████████ ▀▀███▀▀▀▀▀   ███        ▀▀███▀▀▀▀███▀  ▄██   ███{Y}      ███     ▄██▀   ███    ███{R}",
        "  ███    ███ ███   ███   ███    ███ ▀███████████ ███    █▄    ███    ███   ███   ███{Y}      ███     ███    ███    ███{R}",
        "  ███    ███ ███   ███   ███    ███   ███    ███ ███    ███   ███    ███   ███   ███{Y}      ███    ▄██▀    ███    ███{R}",
        "  ███    █▀   ▀█   █▀    ███    █▀    ███    ███ ████████▀    ███    █▀     ▀█████▀ {Y}    ▄█████▄ ▄██▀      ▀██████▀ {R}",
        "                                      ███    ███ ",
        "{0}"
    ];

    private static readonly string[] Compact = [
        "{Y}",
        "",
        "                                   .",
        "                                 =**=",
        "                                +****+",
        "                               +******+",
        "                             -**********-",
        "                            +************-",
        "                          :***************-",
        "                         +#{Y}****************={R}",
        "                        =###{Y}****************+{R}",
        "                       *######{Y}***************+{R}",
        "                     -#########{Y}****************{R}",
        "                    +###########*- {Y}-*****=-:         .:-::::::--:{R}",
        "                   *###########:    :=:     {Y}:-=+======--====+-.{R}",
        "                 =###########+         {Y}:-+*++++++++++++++-:{R}",
        "                =###########:       -*********{Y}++++++-:{R}",
        "               *##########+      =******{Y}+++++===-{R}",
        "             :###########=     =#*****{Y}++++++-.     =+-{R}",
        "            +##########*     =#*******{Y}++=:     -*#****-{R}",
        "          .*##########+    +####{Y}****+:     :+#####*****={R}",
        "         +###########+   :*#####*=         +#######{Y}*****+{R}",
        "        +##########*.   =####*:             +##########{Y}**+{R}",
        "       ###########+   =##*=                  +############{Y}*{R}",
        "     +###########+   ++=                      =############{Y}*:{R}",
        "    +##########*                               =#############{Y}-{R}",
        "  =###########=                                 -#############{Y}={R}",
        " =#####**==                                      -#############{Y}+{R}",
        "=+=.                                              {Y}-++++++***++++-{0}",
        ""
    ];

    /// <summary>
    /// The banner for a terminal <paramref name="columns"/> wide, with ANSI colors unless <paramref name="color"/> is false.
    /// </summary>
    public static string Render(int columns, bool color = true) =>
        string.Join("\n", columns >= WideColumns ? Wide : Compact)
            .Replace("{R}", color ? Red : "")
            .Replace("{Y}", color ? Yellow : "")
            .Replace("{0}", color ? Reset : "") + "\n";
}
