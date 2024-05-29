using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;

namespace S7SourceToXmlUI
{
    internal class XmlFileGenerator
    {
        private static int _lastByte;
        private static int _actByte;
        private static int _lastBit;
        private static int _actBit;
        private static string _lastType;
        private static string _type;
        private static string _nextType;
        private static string _dbname;
        private static bool _endStruct;
        private static int _byteInStruct;

        public static void ReadDataBlockFile(MainWindowViewModel vm, string source, string destination)
        {
            InitializeVariables(vm);

            string xmlfile = "";
            string bmk = "";
            string actualV = "";
            string group = vm.tbBezeichnung;
            const string sep = ".";
            int level = -1;
            int id = 1;
            var nameTree = new string[10];

            var list = File.ReadAllLines(source).ToList();
            int lineCounter = CountLines(list);

            var lines = list.ToArray();
            bool dbstart = false;
            bool read = true;
            vm.pbMaximum = lineCounter;

            for (int i = 0; read && i < lines.Length; i++)
            {
                Console.WriteLine(lines[i]);
                string propName = GetPropertyName(lines[i]);

                if (i == lineCounter - 1)
                {
                    read = false;
                    propName = "END_DATA_BLOCK";
                }

                switch (propName)
                {
                    case "DATA_BLOCK":
                        xmlfile = CreateDataBlockXml(destination);
                        CreateXmlStart(destination, xmlfile);
                        break;
                    case "VAR":
                    case "STRUCT":
                    case ": STRUCT":
                        level++;
                        dbstart = true;
                        if (propName == ": STRUCT")
                            nameTree[level] = sep + lines[i].Substring(0, lines[i].IndexOf(":", StringComparison.Ordinal)).Trim();
                        break;
                    case "END_STRUCT":
                        ResetNameTree(nameTree, ref level);
                        if (level == -1) dbstart = false;
                        break;
                    case "END_VAR":
                        level--;
                        dbstart = false;
                        break;
                    case "ARRAY":
                        ProcessArray(lines[i], ref id, destination, xmlfile, group, nameTree, sep, actualV, bmk);
                        break;
                    case "Node":
                        ProcessNode(lines, i, ref id, destination, xmlfile, group, nameTree, sep, actualV, bmk);
                        break;
                    case "END_DATA_BLOCK":
                        CreateXmlEnd(destination, xmlfile);
                        vm.pbValue = lines.Length;
                        vm.tblProgress = "Konvertierung abgeschlossen";
                        return;
                }
                vm.pbValue = i + 1;
                vm.tbAusgabe += $"{i + 1}: {lines[i]}{Environment.NewLine}";
            }
        }

        private static void InitializeVariables(MainWindowViewModel vm)
        {
            _lastByte = 0;
            _actByte = 0;
            _lastBit = 0;
            _actBit = 0;
            _lastType = "";
            _type = "";
            _nextType = "";
            _dbname = vm.tbDBName;
        }

        private static int CountLines(IEnumerable<string> list)
        {
            return list.TakeWhile(item => !item.Contains("END_DATA_BLOCK") && !item.Contains("BEGIN")).Count();
        }

        private static string GetPropertyName(string line)
        {
            string trimmedLine = line.ToUpper().Trim();
            if (trimmedLine.Contains("DATA_BLOCK")) return "DATA_BLOCK";
            if (trimmedLine == "VAR") return "VAR";
            if (trimmedLine == "STRUCT") return "STRUCT";
            if (trimmedLine.Contains(": STRUCT")) return ": STRUCT";
            if (trimmedLine.Contains("END_STRUCT")) return "END_STRUCT";
            if (trimmedLine == "END_VAR") return "END_VAR";
            if (trimmedLine.Contains(": ARRAY")) return "ARRAY";
            if (!trimmedLine.Contains("END_STRUCT") && !trimmedLine.Contains("STRUCT") &&
                !trimmedLine.Contains(": ARRAY") && !trimmedLine.Contains("VAR"))
                return "Node";
            return string.Empty;
        }

        private static void ResetNameTree(string[] nameTree, ref int level)
        {
            nameTree[level + 1] = "";
            nameTree[level] = "";
            level--;
            _byteInStruct = 0;
        }

        private static void ProcessArray(string line, ref int id, string destination, string xmlfile, string group,
            string[] nameTree, string sep, string actualV, string bmk)
        {
            int startLength = int.Parse(line.Split('[')[1].Split('.')[0].Trim());
            int length = int.Parse(line.Split('.')[2].Split(']')[0].Trim());
            for (int j = 0; j <= length - startLength; j++)
            {
                string node = line.Split(':')[0].Trim() + "_" + j;
                _type = ExtractType(line);
                string comment = "Reserve";
                string tempName = BuildTempName(node, nameTree, sep);

                var array = GetStartAddress(_type);
                CreateXmlElement(destination, xmlfile, id.ToString(), group, tempName, array[0].ToString(), array[1].ToString(),
                    _type, "0", actualV, bmk, comment);

                id++;
            }
        }

        private static void ProcessNode(string[] lines, int i, ref int id, string destination, string xmlfile, string group,
            string[] nameTree, string sep, string actualV, string bmk)
        {
            string node = lines[i].Split(':')[0].Trim();

            if (i > -1)
            {
                _type = ExtractNodeType(lines[i]);
                _nextType = GetNextType(lines, i);
                string comment = ExtractComment(lines[i]);
                bmk = ExtractLocation(comment);

                string tempName = BuildTempName(node, nameTree, sep);
                string defaultV = ExtractDefaultValue(lines, tempName);

                var array = GetStartAddress(_type);
                CreateXmlElement(destination, xmlfile, id.ToString(), group, tempName, array[0].ToString(), array[1].ToString(),
                    _type, defaultV, actualV, bmk, comment);

                id++;
            }
        }

        private static string ExtractType(string line)
        {
            try
            {
                return line.Split('f')[1].Split(';')[0].Trim();
            }
            catch
            {
                Console.WriteLine("Kein Typ gefunden");
                return string.Empty;
            }
        }

        private static string ExtractNodeType(string line)
        {
            if (line.ToUpper().Contains("REAL")) return "REAL";
            if (line.ToUpper().Contains("BOOL")) return "BOOL";
            if (line.ToUpper().Contains("INT")) return "INT";
            if (line.ToUpper().Contains("DINT")) return "DINT";
            if (line.ToUpper().Contains("WORD")) return "WORD";
            if (line.ToUpper().Contains("DWORD")) return "DWORD";
            if (line.ToUpper().Contains("BYTE")) return "BYTE";
            return string.Empty;
        }

        private static string GetNextType(string[] lines, int i)
        {
            try
            {
                _endStruct = lines[i + 1].ToUpper().Contains("END_STRUCT");
                return lines[i + 1].Split(':')[1].Split(';')[0].Trim().ToUpper();
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string ExtractComment(string line)
        {
            try
            {
                return line.Split(new[] { "//" }, StringSplitOptions.None)[1].Trim();
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string ExtractLocation(string comment)
        {
            try
            {
                return "=" + comment.Split('=')[1].Split(' ')[0].Trim();
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string BuildTempName(string node, string[] nameTree, string sep)
        {
            string tempName = "";
            nameTree[9] = sep + node;
            foreach (var t in nameTree)
                tempName += t;

            return tempName.Substring(1).Trim();
        }

        private static string ExtractDefaultValue(string[] lines, string tempName)
        {
            foreach (string line in lines)
            {
                if (line.Split(':')[0].Trim() == tempName)
                {
                    try
                    {
                        return line.Split(':=')[1].Split(';')[0].Trim();
                    }
                    catch
                    {
                        return string.Empty;
                    }
                }
            }
            return string.Empty;
        }

        private static string CreateDataBlockXml(string destination)
        {
            string xmlfile = Path.Combine(destination, "AWL_DB_" + _dbname + ".xml");
            using (var sw = File.CreateText(xmlfile))
            {
                sw.WriteLine("<Root>");
            }
            return xmlfile;
        }

        private static void CreateXmlStart(string destination, string xmlfile)
        {
            using (var sw = File.AppendText(xmlfile))
            {
                sw.WriteLine("<DB>" + _dbname + "</DB>");
            }
        }

        private static void CreateXmlElement(string destination, string xmlfile, string id, string group, string name, string address,
            string bit, string type, string defaultV, string actualV, string bmk, string comment)
        {
            using (var sw = File.AppendText(xmlfile))
            {
                sw.WriteLine($"<Element ID=\"{id}\" Group=\"{group}\" Name=\"{name}\" Address=\"{address}\" Bit=\"{bit}\" " +
                             $"Type=\"{type}\" Default=\"{defaultV}\" Actual=\"{actualV}\" BMK=\"{bmk}\" Comment=\"{comment}\" />");
            }
        }

        private static void CreateXmlEnd(string destination, string xmlfile)
        {
            using (var sw = File.AppendText(xmlfile))
            {
                sw.WriteLine("</Root>");
            }
        }

        private static int[] GetStartAddress(string type)
        {
            int[] result = new int[2];

            switch (type)
            {
                case "REAL":
                case "DWORD":
                    result[0] = _actByte;
                    result[1] = 0;
                    _actByte += 4;
                    break;
                case "INT":
                case "WORD":
                    result[0] = _actByte;
                    result[1] = 0;
                    _actByte += 2;
                    break;
                case "BOOL":
                    if (_actBit > 7)
                    {
                        _actByte++;
                        _actBit = 0;
                    }
                    result[0] = _actByte;
                    result[1] = _actBit;
                    _actBit++;
                    break;
                case "BYTE":
                    result[0] = _actByte;
                    result[1] = 0;
                    _actByte++;
                    break;
                default:
                    throw new InvalidOperationException($"Unknown type encountered: {type}");
            }

            if (_endStruct && _nextType != type && (_nextType == "REAL" || _nextType == "DWORD" || _nextType == "INT" ||
                _nextType == "WORD" || _nextType == "BOOL" || _nextType == "BYTE"))
            {
                _actByte = ((int)Math.Ceiling(_actByte / 4.0)) * 4;
                _endStruct = false;
            }

            _lastByte = _actByte;
            _lastBit = _actBit;
            _lastType = type;

            return result;
        }
    }
}
