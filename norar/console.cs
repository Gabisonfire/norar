using System;

namespace norar
{
    class console
    {
        public enum msgType { standard, error, warning, system};


        /// <summary>
        /// Write to console with predefined header
        /// </summary>
        /// <param name="msg">The message to display</param>
        /// <param name="msgtype">the type of message to display</param>
        /// <param name="newline">End with a new line?</param>
        /// <param name="log">Log the message to log file?</param>
        public static void Write(string msg, msgType msgtype = msgType.standard, bool newline = true, bool log = true)        
        {
            switch (msgtype)
            {
                case msgType.standard:
                    {
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.Write("[INFO] :: ");
                        if (log)
                            io.Log(msg);
                        break;
                    }
                case msgType.system:
                    {
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.Write("[SYSTEM] :: ");
                        if (log)
                            io.Log(msg);
                        break;
                    }
                case msgType.warning:
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write("[WARNING] :: ");
                        if (log)
                            io.Log(msg);
                        main.warn_count++;
                        break;
                    }
                case msgType.error:
                    {
                        // Errors are not logged because we log the full errors.
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write("[ERROR] :: ");
                        main.err_count++;
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
            if (newline)
                Console.WriteLine(msg);
            else
                Console.Write(msg);            
            Console.ForegroundColor = ConsoleColor.White;
                    
        }

        public static void Exit(int code)
        {
            if (code != 0)
            {
                Write("Exiting with code " + code.ToString() + "Errors: " + main.err_count.ToString() + " Warnings: " + main.warn_count.ToString(), msgType.system);
            }
            else
            {
                Write("Exiting normally. Errors: " + main.err_count.ToString() + " Warnings: " + main.warn_count.ToString(), msgType.system);
            }
            if (main.halt)
            {
                Console.Read();
            }
            Environment.Exit(code);
        }

    }

}
