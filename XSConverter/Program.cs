using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Karambolo.PO;
using XSConverter.Format;

namespace XSConverter
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                if (args[0].ToLower() == "findchars")
                {
                    var poDir = args[1];
                    Console.WriteLine("Finding PO Files From " + poDir + "...");
                    var poFiles = Directory.GetFiles(poDir, "*.po");
                    var charList = new List<char>();
                    var parser = new POParser();
                    foreach (String poPath in poFiles)
                    {
                        Console.WriteLine("Processing " + poPath + "...");
                        var poFile = File.OpenRead(poPath);
                        var parsed = parser.Parse(poFile);
                        foreach (IPOEntry entry in parsed.Catalog)
                        {
                            foreach (String trans in entry)
                            {
                                foreach (char c in trans.ToCharArray())
                                {
                                    if(!charList.Contains(c))
                                    {
                                        charList.Add(c);
                                    }
                                }
                            }
                        }
                    }
                    Console.WriteLine("Found " + charList.Count + " chars.");
                    var outputFile = new StreamWriter(args[2], false, Encoding.UTF8, 8192);
                    var options = new JsonSerializerOptions();
                    options.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
                    var doc = JsonSerializer.Serialize(charList, options);
                    outputFile.WriteLine(doc);
                    outputFile.Close();

                }
                else if (args[0].ToLower() == "topo")
                {
                    var xsDir = args[1];
                    var poDir = args[2];
                }
                else if (args[0].ToLower() == "toxs" && args.Length == 5)
                {
                    var poDir = args[1];
                    var xsDir = args[2];
                    var tblFile = File.Open(args[3], FileMode.Open);
                    string tblJson;
                    StreamReader reader = new StreamReader(tblFile);
                    tblJson = reader.ReadToEnd();
                    Dictionary<string, string> tbl = (Dictionary<string, string>) JsonSerializer.Deserialize(tblJson, typeof(Dictionary<string, string>));
                    var outputDir = args[4];
                    Console.WriteLine("[INFO] Finding PO Files From " + poDir + "...");
                    var poFiles = Directory.GetFiles(poDir, "*.po");
                    var parser = new POParser();
                    foreach (String poPath in poFiles)
                    {
                        Console.WriteLine("[INFO] Processing " + poPath + "...");
                        var targetXsDirs = Directory.GetDirectories(xsDir).Where(path => {
                            return Path.GetFileName(path) == Path.GetFileNameWithoutExtension(poPath);
                        });
                        
                        if (targetXsDirs.Count() != 1)
                        {
                            Console.WriteLine("[WARN] Cannot find matched directory with " + poPath + ". Skipping...");
                            continue;
                        }
                        var targetXsDir = targetXsDirs.First();
                        var poFile = File.OpenRead(poPath);
                        var parsed = parser.Parse(poFile);
                        var files = new Dictionary<string, XS>();
                        foreach (IPOEntry entry in parsed.Catalog)
                        {
                            var contextId = entry.Key.ContextId.Split(':');
                            var xsFileName = contextId[0];
                            var entryIndex = contextId[1];
                            var subEntryIndex = contextId[2];
                            if(entry.Count() != 1)
                            {
                                Console.WriteLine("[WARN] Invaild Entry " + entry.Key.ContextId + ". Skipping...");
                                continue;
                            }
                            if (entry[0] == "")
                            {
                                //Console.WriteLine("[WARN] Empty Entry " + entry.Key.ContextId + ". Skipping...");
                                continue;
                            }
                            var trans = entry[0].Trim().Trim('\n', '\r');
                            foreach (KeyValuePair<string, string> tblChar in tbl)
                            {
                                trans = trans.Replace(tblChar.Value, tblChar.Key);
                            }
                            if (!files.ContainsKey(xsFileName))
                            {
                                if (!File.Exists(targetXsDir + Path.DirectorySeparatorChar + xsFileName))
                                {
                                    Console.WriteLine("[WARN] File " + targetXsDir + Path.DirectorySeparatorChar + xsFileName + " is not exist. Skipping...");
                                    continue;
                                }
                                files.Add(xsFileName, new XS(targetXsDir + Path.DirectorySeparatorChar + xsFileName));
                            }
                            var xs = files[xsFileName];
                            var targetLabels = xs.Labels.Where(label => { return label.Name == (entryIndex + ":" + subEntryIndex); });
                            if (targetLabels.Count() != 1)
                            {
                                Console.WriteLine("[WARN] Entry on XS " + entry.Key.ContextId + " is not exist. Skipping...");
                                continue;
                            }
                            var targetLabel = targetLabels.First();
                            targetLabel.Text = trans;
                        }
                        Console.WriteLine("[INFO] Saving " + Path.GetFileNameWithoutExtension(poPath) + " Files...");
                        foreach (KeyValuePair<string, XS> file in files)
                        {
                            if (!Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);
                            if (!Directory.Exists(outputDir + Path.DirectorySeparatorChar + Path.GetFileNameWithoutExtension(poPath))) Directory.CreateDirectory(outputDir + Path.DirectorySeparatorChar + Path.GetFileNameWithoutExtension(poPath));
                            file.Value.Save(outputDir + Path.DirectorySeparatorChar + Path.GetFileNameWithoutExtension(poPath) + Path.DirectorySeparatorChar + file.Key);
                        }
                    }
                }
                else
                {
                    Console.WriteLine("XSConverter <findchars|topo|toxs> <args...>");
                    Console.WriteLine("Commands");
                    Console.WriteLine("    findchars <poDir> <outputJson> - find used characters on selected po files and export to json.");
                    Console.WriteLine("    topo <xsDir> <poDir> - Export XS Files To Gettext PO Files.");
                    Console.WriteLine("    toxs <poDir> <xsDir> <tblJson> <outputDir> - Import Gettext PO Files to XS Files.");
                }
            }
            else
            {
                Console.WriteLine("XSConverter <findchars|topo|toxs> <args...>");
                Console.WriteLine("Commands");
                Console.WriteLine("    findchars <poDir> <outputJson> - find used characters on selected po files and export to json.");
                Console.WriteLine("    topo <xsDir> <poDir> - Export XS Files To Gettext PO Files.");
                Console.WriteLine("    toxs <poDir> <xsDir> <tblJson> <outputDir> - Import Gettext PO Files to XS Files.");
            }
        }
    }
}
