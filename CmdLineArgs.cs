using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLineUtility;

namespace GenerateJsonTableSchema;

public class CmdLineArgs
{
    const bool REQUIRED = true;
    const bool OPTIONAL = false;

    [CmdArg("--databasename", "-d", REQUIRED, "SQL Server database name")]
    public string databasename { get; set; }

} 