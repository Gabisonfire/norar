using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using SevenZip;
using System.Security.Cryptography;
using System.Text.RegularExpressions;


namespace norar
{
    class io
    {

        public const string HASH_FILE = "_hashes";

        /// <summary>
        /// Decompresses an archive.
        /// </summary>
        /// <param name="archive">Path for the archive</param>
        /// <param name="destination">Archive content destination folder.</param>
        /// <returns>True if no errors</returns>
        public static bool Decompress(string archive, string destination)
        {
            try {
                string archive_name = Path.GetFileNameWithoutExtension(archive);
                if(main.full_path_output)
                    console.Write("Extracting " + archive + " to " + destination + " ->  0%", console.msgType.standard, false);
                else
                    console.Write("Extracting " + archive_name + " to " + destination + " ->  0%", console.msgType.standard, false);

                SevenZipExtractor zip = new SevenZipExtractor(archive);
                zip.Extracting += Zip_Extracting;
                if (!main.overwrite)
                    zip.FileExists += Zip_FileExists;
                if (main.create_dir)
                {                                       
                    zip.ExtractArchive(destination + @"\" + archive_name);
                }
                else
                    zip.ExtractArchive(destination);
                Console.WriteLine("");
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("");
                console.Write(e.Message +"("+archive+")", console.msgType.error);
                Log(e.ToString());
                return false;
            }
        }

        // Event handler for file overwriting
        private static void Zip_FileExists(object sender, FileOverwriteEventArgs e)
        {
            Console.WriteLine("");
            console.Write("Destination exists already. Skipping.", console.msgType.warning);
            e.Cancel = true;
        }

        // Event handler to display progress progress
        private static void Zip_Extracting(object sender, ProgressEventArgs e)
        {
            if (e.PercentDone < 10)
                Console.Write("\b\b{0}%", e.PercentDone);
            else
                Console.Write("\b\b\b{0}%", e.PercentDone);
        }

        /// <summary>
        /// Creates a list of file paths to be processed.
        /// </summary>
        /// <param name="directory">The directory to scan files</param>
        /// <param name="extensions">The extensions to filter</param>
        /// <param name="recursive">Wheter or not the program will scan the subfolders</param>
        /// <returns>Array of strings containing file paths.</returns>
        public static string[] fileList(string directory, string[] extensions, bool recursive = true)
        {
            if (!Directory.Exists(directory))
            {
                console.Write("Directory does not exist!", console.msgType.error);
                console.Exit(1);
            }
            List<string> result = new List<string>();
            string[] buffer;
            try {
                if (recursive) { buffer = Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories); }
                else { buffer = Directory.GetFiles(directory, "*.*"); }
                foreach (string path in buffer)
                {
                    foreach (string ext in extensions)
                    {
                        // Filter extensions
                        if (path.EndsWith("." + ext))
                        {
                            // Check if regex exclude is on
                            if (main.rgx && main.rgx_patttern != null)
                            {
                                Regex regex = new Regex(main.rgx_patttern);
                                Match match = regex.Match(path);
                                if (!match.Success)
                                {
                                    result.Add(path);
                                }
                            }
                            else
                                result.Add(path);
                        }
                    }
                }
            }
            catch(Exception e)
            {
                console.Write(e.Message, console.msgType.error);
                Log(e.ToString());
            }
            return result.ToArray();
        }

        /// <summary>
        /// Computes file md5 and stores it in a file
        /// </summary>
        /// <param name="file">The path tp the file to hash</param>
        public static void StoreMD5(string file)
        {
            if (main.dryrun) return;
            try {
                using (var md5 = MD5.Create())
                {
                    using (var stream = File.OpenRead(file))
                    {
                        File.AppendAllText(HASH_FILE, Path.GetFileName(file) + ":" + BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLower() + Environment.NewLine);
                        Log("Storing MD5 for " + file);
                    }
                }
            }
            catch (Exception e)
            {
                console.Write("Could not compute MD5. " + e.Message, console.msgType.error);
                Log(e.ToString());
            }
        }

        /// <summary>
        /// Builds the Dicitonary for searching file hashes.
        /// </summary>
        /// <returns>Dictionary containing file names and hashes.</returns>
        public static Dictionary<string, string> buildHashDict()
        {
            if (!File.Exists(HASH_FILE))
            {
                console.Write("No hashes file found.", console.msgType.warning);
                return null;
            }
            Dictionary<string, string> buffer = new Dictionary<string, string>();
            string[] hashes = File.ReadAllLines(HASH_FILE);
            foreach (string hash in hashes)
            {
                int i = hash.IndexOf(":");
                if (i == -1) continue;
                string file_name = hash.Substring(0, i);
                string file_hash = hash.Substring(i + 1, hash.Length - i - 1);
                try
                {
                    buffer.Add(file_name, file_hash);
                }
                catch (Exception e)
                {
                    console.Write("An error occured building hashes dictionary, check the logfile.", console.msgType.error);
                    Log("ERROR: " + e.Message);
                }
            }
            return buffer;
        }

        /// <summary>
        /// Match hash in the provided dictionary
        /// </summary>
        /// <param name="hashes">Dictionary with hashes and file names</param>
        /// <param name="file">The file to check against</param>
        /// <returns>True if a match is found</returns>
        public static bool matchHash(Dictionary<string, string> hashes, string file)
        {
            if (hashes == null) return false;
            // Get file hash
            string file_hash;
            try {
                using (var md5 = MD5.Create())
                {
                    using (var stream = File.OpenRead(file))
                    {
                        file_hash = BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLower();
                    }
                }

                // Compare
                foreach (var pair in hashes)
                {
                    if (pair.Value == file_hash)
                        return true;
                }
            }
            catch (Exception e)
            {
                Log("ERROR: " + e.Message);
            }
            return false;
        }

        /// <summary>
        /// Match filename in provided dicitonary
        /// </summary>
        /// <param name="hashes">Dictionary with hashes and file names</param>
        /// <param name="file">The file to check against</param>
        /// <returns>True if a match is found</returns>
        public static bool matchFileName(Dictionary<string, string> hashes, string file)
        {
            try
            {
                string file_name = Path.GetFileName(file);
                foreach (var pair in hashes)
                {
                    if (pair.Key == file_name)
                        return true;
                }
            }
            catch (Exception e)
            {
                Log("ERROR: " + e.ToString());
            }
            return false;
        }

        // Truncate filename to try and reduce the output when creating folders.
        public static string truncate(string filename)
        {
            return filename.Trim().Replace(" ", "");
        }

        // Read config and apply values and switches
        public static void readConfig(string config)
        {
            if(!File.Exists(config))
            {
                console.Write("Unable to find config file " + config, console.msgType.error);
                console.Exit(3);
            }
            string[] cfg = File.ReadAllLines(config);
            if(cfg.Count() < 3)
            {
                console.Write("Invalid config file.", console.msgType.error);
                console.Exit(4);
            }
            main.top_dir = cfg[0];
            main.dest_dir = cfg[1];
            main.ext_str = cfg[2];
            main.checkSwitches(cfg);

        }

        // Will only display files in console, used with dryrun
        public static void displayFiles(string[] files)
        {
            foreach (string file in files)
            {
                console.Write(file, console.msgType.standard, true, false);
            }
        }

        /// <summary>
        /// Checks if a file reaches his max size, if it does, creates a new empty one.
        /// </summary>
        /// <param name="path">Path of the file to purge</param>
        /// <param name="maxSize">Max size in MB</param>
        public static void filePurge(string path, int maxSize)
        {
            if (!File.Exists(path)) return;
            console.Write("Checking log size for purge...", console.msgType.standard, false);
            long size = new FileInfo(path).Length;
            long maxSizeBytes = maxSize * 1000000;
            if(size > maxSizeBytes)
            {
                Console.WriteLine("Purging needed");
                string nowstr = DateTime.Now.ToString("_MM_dd_yy");
                try {
                    if(main.log_move)
                        File.Move(path, path + nowstr + ".bkp");
                    else
                        File.Delete(path);
                    using (File.Create(path)) { };
                    console.Write("Done.");
                }
                catch(Exception e)
                {
                    console.Write(e.Message, console.msgType.error);
                    Log(e.ToString());
                }
            }
            else
                Console.WriteLine("OK");
        }

        /// <summary>
        /// Checks if a file reaches his max size, if it does, creates a new one with the last lines from the old one.
        /// </summary>
        /// <param name="path">Path of the file to purge</param>
        /// <param name="maxSize">Max size in MB</param>
        /// <param name="keepLines">How many entries to keep from old file to the new file.</param>
        public static void filePurge(string path, int maxSize, int keepLines)
        {
            if (!File.Exists(path)) return;
            console.Write("Checking hashes size for purge...", console.msgType.standard, false);
            long size = new FileInfo(path).Length;
            long maxSizeBytes = maxSize * 1000000;
            if (size > maxSizeBytes)
            {
                try {
                    Console.WriteLine("Purging needed");
                    string[] original = File.ReadAllLines(path);
                    List<string> result = new List<string>();
                    int count = 0;
                    for (int i = original.Length - 1; i >= 0; i--)
                    {
                        if (count >= keepLines) break;                        
                        result.Insert(0, original[i]);
                        count++;
                    }
                    DateTime now = DateTime.Now;
                    string nowstr = now.ToString("_MM_dd_yy");
                    if (main.hash_move)
                        File.Move(path, path + nowstr + ".bkp");
                    else
                        File.Delete(path);
                        File.WriteAllLines(path, result);
                    console.Write("Done.");
                }
                catch(Exception e)
                {
                    console.Write(e.Message, console.msgType.error);
                }
            }
            else
                Console.WriteLine("OK");
        }

        // Logs to the defined log file.
        public static void Log(string msg)
        {
            string now = DateTime.Now.ToString("MM-dd-yy || hh:mm:ss");
            try
            {
                if(!Directory.Exists(main.log_path)) { Directory.CreateDirectory(main.log_path); }
                File.AppendAllText(main.log_path + "norar.log", "[" + now + "] " + msg + Environment.NewLine);
            }
            catch (Exception e)
            {
                console.Write(e.Message, console.msgType.error, true, false);
            }
        }

    }
}
