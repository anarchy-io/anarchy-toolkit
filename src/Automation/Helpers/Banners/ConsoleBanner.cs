namespace Anarchy.Toolkit.Automation.Helpers.Banners;

/// <summary>
/// Anarchy's logo, as printed by <c>scripts/console/banner.sh</c>: the wide wordmark on terminals of at least
/// <see cref="WideColumns"/> columns, the compact emblem on terminals of at least <see cref="CompactColumns"/>, and
/// a plain <see cref="Title"/> on narrower ones.
/// </summary>
public static class ConsoleBanner
{
    /// <summary>
    /// The minimum terminal width, in columns, for the wide wordmark.
    /// </summary>
    public const int WideColumns = 116;

    /// <summary>
    /// The minimum terminal width, in columns, for the compact emblem; narrower terminals get the <see cref="Title"/>.
    /// </summary>
    public const int CompactColumns = 66;

    /// <summary>
    /// The plain title printed instead of the logo on terminals narrower than <see cref="CompactColumns"/>.
    /// </summary>
    public const string Title = "ANARCHY I/O TOOLKIT";

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
    /// <param name="columns">The terminal width, in columns.</param>
    /// <param name="color">Whether to include ANSI color escape sequences.</param>
    /// <returns>The banner, or the <see cref="Title"/> between blank lines below <see cref="CompactColumns"/>.</returns>
    public static string Render(int columns, bool color = true) => columns < CompactColumns
        ? color ? "\n\e[1;30m ANARCHY I/\e[1;31mO \e[1;33mTOOLKIT\e[0m\n\n" : $"\n {Title}\n\n"
        :
        string.Join("\n", columns >= WideColumns ? Wide : Compact)
            .Replace("{R}", color ? Red : "")
            .Replace("{Y}", color ? Yellow : "")
            .Replace("{0}", color ? Reset : "") + "\n";
}
