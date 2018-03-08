using Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace REDCapIMPROVE
{
    class Program
    {
        private static Dictionary<string, string> postArgs;
        private static Dictionary<string, string> config;

        static void Main(string[] args)
        {
            string logPath = Path.Combine(Directory.GetCurrentDirectory(), "log");
            string logName = DateTime.Now.Year.ToString() + DateTime.Now.Month.ToString().PadLeft(2, '0') + DateTime.Now.Day.ToString().PadLeft(2, '0') + ".log";
            Logger log = new Logger(logPath);

            postArgs = new Dictionary<string, string>();

            foreach (string argument in args)
            {
                string argumentClean = HttpUtility.UrlDecode(argument);

                var split = argumentClean.Split(new char[] { '=' }, 2, StringSplitOptions.RemoveEmptyEntries);

                if (split.Length >= 2) postArgs.Add(split[0], split[1]);
            }

            try
            {
                string configPath = Path.Combine(Environment.CurrentDirectory, "config.txt");
                config = LoadConfig(configPath);
                string decryptionKey = config["DecryptPassword"];
                config["strURI"] = Encryption.Cypher.Decrypt(config["strURI"], decryptionKey);
                config["strPostToken"] = Encryption.Cypher.Decrypt(config["strPostToken"], decryptionKey);
                config["DataSource"] = Encryption.Cypher.Decrypt(config["DataSource"], decryptionKey);
                config["UserID"] = Encryption.Cypher.Decrypt(config["UserID"], decryptionKey);
                config["Password"] = Encryption.Cypher.Decrypt(config["Password"], decryptionKey);

            }
            catch (Exception ex)
            {
                log.insertSectionSeperator("ERROR OCCURED");
                log.Log(ex.ToString());
            }
            finally
            {
                log.Dispose();
            }
        }

        private static Dictionary<string, string> LoadConfig(string configPath)
        {
            Dictionary<string, string> config = new Dictionary<string, string>();
            using (StreamReader sr = new StreamReader(configPath, System.Text.Encoding.Default))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    if (line.Contains("="))
                    {
                        string[] parameter = line.Split(new char[] { '=' }, 2, StringSplitOptions.RemoveEmptyEntries);

                        config.Add(parameter[0], parameter[1]);
                    }
                }
            }
            return config;
        }
    }
}
