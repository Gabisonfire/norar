using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

/* TODO
Agent/Service
Long file names
*/

namespace norar
{
    class main
    {
        // Global Variables
        public static bool create_dir = false;              // Creates a subfolder when extracting
        public static bool full_path_output = false;        // Shows the full path in the console when extracting.
        public static bool recursive_search = false;        // Searches the top_dir recursively
        public static bool overwrite = false;               // Overwrite existing files while extracting
        public static bool truncate = false;                // Will truncate too long destination file/folder names.
        public static matchType match = matchType.hash;     // By default will match only the file hash.
        public static bool force = false;                   // Force reprocessing even if file exists in hashes.
        public static bool rgx = false;                     // Exclude regex pattern processor
        public static bool use_config = false;              // Use config file or not.
        public static bool halt = false;                    // Halt the program before exiting
        public static bool dryrun = false;                  // Toggles test runs of the application
        public static bool log_move = false;                // Toggles to move log files instead of deleting them.
        public static bool hash_move = false;               // Toggles to move hash files instead of deleting them.
        public static bool build_hashes = false;            // Will only build a hashes file. (Used for 1st run)


        // Used to store file names and hashes.
        public static Dictionary<string, string> hashes = new Dictionary<string, string>(); 


        public enum matchType { both, hash, name };         // Enum to assign a match type for files        

        public static string top_dir = null;                // Top directory for searching
        public static string dest_dir = null;               // Destination directory for content
        public static string ext_str = null;                // Extensions to include
        public static string rgx_patttern = null;           // Stores the regex pattern to match
        public static string cfg_path = null;               // Stores the path to the config file
        public static string log_path = @"logs\";           // Path for the log file

        public static int err_count = 0;                    // Keep errors count
        public static int warn_count = 0;                   // Keep warnings count
        public static int purge_size = 10;                  // Max hashes file size
        public static int purge_keep = 200;                 // How many hashes will be kept.
        public static int log_size = 10;                    // Max size of log files.


        public const string VER = "0.2";

        static void Main(string[] args)
        {            

            // Header
            Console.WriteLine("");      
            console.Write(@" _ __   ___  _ __ __ _ _ __ ", console.msgType.standard, true, false);
            console.Write(@"| '_ \ / _ \| '__/ _` | '__|", console.msgType.standard, true, false);
            console.Write(@"| | | | (_) | | | (_| | | ", console.msgType.standard, true, false);
            console.Write(@"|_| |_|\___/|_|  \__,_|_| " + VER, console.msgType.standard, true, false);
            console.Write(@"", console.msgType.standard, true, false);
            Console.WriteLine(@"");
            
            console.Write("Initializing...");

            // Console arguments handling
            handleArguments(args);

            // Apply switches if config is not used only.
            if (!use_config)
            {
                // Apply switches
                checkSwitches(args);
            }

            io.Log("Application Started.");

            // If build hashes mode, just create hashes file.
            if(build_hashes)
            {
                buildHashes();
                console.Exit(0);
            }

            // Purge hashes and log files if needed.
            io.filePurge(io.HASH_FILE, purge_size, purge_keep);
            io.filePurge(log_path + "norar.log", log_size);

            // Build hashes tables
            hashes = io.buildHashDict();

            // List files
            string[] files = io.fileList(top_dir, ext_str.Split(','), recursive_search);
            console.Write(files.Length.ToString() + " files found.");

            // startDecompression process
            if (!dryrun)
                startDecompress(files);
            else
                io.displayFiles(files);     
            
            // Exit cleanly                    
            console.Exit(0);
            
        }


        // Check switches and apply to global triggers.
        public static void checkSwitches(string[] args)
        {
            // Check for switches
            foreach (string arg in args)
            {
                if (arg == "/dir") create_dir = true;
                if (arg == "/truncate") truncate = true;
                if (arg == "/fullpath") full_path_output = true;
                if (arg == "/recursive") recursive_search = true;
                if (arg == "/overwrite") overwrite = true;
                if (arg == "/matchfilename") match = matchType.name;
                if (arg == "/force") force = true;
                if (arg == "/halt") halt = true;
                if (arg == "/dryrun") dryrun = true;
                if (arg == "/movelog") log_move = true;
                if (arg == "/movehashes") hash_move = true;
                if (arg == "/buildhashes") build_hashes = true;

                if (arg.StartsWith("/regexclude:"))
                {
                    rgx = true;
                    int i = arg.IndexOf(":");
                    rgx_patttern = arg.Substring(i + 1, arg.Length - i - 1);

                }
                if (arg.StartsWith("/purgesize:"))
                {
                    int i = arg.IndexOf(":");
                    string buffer = arg.Substring(i + 1, arg.Length - i - 1);
                    int size = 0;
                    if(Int32.TryParse(buffer, out size))
                    {
                        purge_size = size;
                    }
                }
                if (arg.StartsWith("/purgebacklog:"))
                {
                    int i = arg.IndexOf(":");
                    string buffer = arg.Substring(i + 1, arg.Length - i - 1);
                    int size = 0;
                    if (Int32.TryParse(buffer, out size))
                    {
                        purge_keep = size;
                    }
                }
                if (arg.StartsWith("/logsize:"))
                {
                    int i = arg.IndexOf(":");
                    string buffer = arg.Substring(i + 1, arg.Length - i - 1);
                    int size = 0;
                    if (Int32.TryParse(buffer, out size))
                    {
                        log_size = size;
                    }
                }
                if (arg.StartsWith("/logpath:"))
                {
                    int i = arg.IndexOf(":");
                    log_path = arg.Substring(i + 1, arg.Length - i - 1);
                }

            }
        }

        // Starts the decompressing process
        static void startDecompress(string[] files)
        {
            foreach (string file in files)
            {
                // If not found in hashes by file hash
                if (match == matchType.hash)
                {
                    if (!io.matchHash(hashes, file) || force)
                    {
                        io.Decompress(file, dest_dir);
                        if (!force)
                            io.StoreMD5(file);
                    }
                    else
                    {
                        if (full_path_output)
                            console.Write("Skipping " + file + ". Already processed.");
                        else
                            console.Write("Skipping " + Path.GetFileName(file) + ". Already processed.");
                    }
                }
                // If not found in hashes by file name
                else if (match == matchType.name)
                {
                    if (!io.matchFileName(hashes, file) || force)
                    {
                        io.Decompress(file, dest_dir);
                        if (!force)
                            io.StoreMD5(file);
                    }
                    else
                    {
                        if (full_path_output)
                            console.Write("Skipping " + file + ". Already processed.");
                        else
                            console.Write("Skipping " + Path.GetFileName(file) + ". Already processed.");
                    }
                }

            }
        }

        // Handles console arguments
        static void handleArguments(string[] args)
        {
            if (args.Count() == 0)
            {
                console.Write("No arguments were provided. Check the readme.", console.msgType.error);
                Console.Read();
                console.Exit(2);
            }
            if (args.Count() == 2)
            {
                if (args[0] == "usecfg")
                {
                    use_config = true;
                    cfg_path = args[1];
                    io.readConfig(cfg_path);
                }
                else
                {
                    console.Write("Invalid arguments.", console.msgType.error);
                    console.Exit(2);
                }
            }

            else if (args.Count() >= 3)
            {
                top_dir = args[0];
                dest_dir = args[1];
                ext_str = args[2];
            }

            else
            {
                console.Write("Invalid arguments.", console.msgType.error);
                console.Exit(2);
            }
        }
        
        // Build hashes file
        static void buildHashes()
        {
            console.Write("Building hashes file...", console.msgType.standard, false);
            string[] temp_files = io.fileList(top_dir, ext_str.Split(','), recursive_search);
            foreach (string file in temp_files)
            {
                io.StoreMD5(file);
            }
            Console.WriteLine("OK");
        }
    }
}
