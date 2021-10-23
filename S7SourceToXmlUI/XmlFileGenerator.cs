using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;

namespace S7SourceToXmlUI
{
    // **************************************************************
    // Zweck:               Anwendung zur Konvertierung von einer .db-Datei aus TIA nach XML
    //                      zum Einlesen in eine HMI
    // Autor:               Sareika, Simon
    // Letze Aenderung:     23.05.2019
    // **************************************************************
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

        /// <summary>
        ///     Liest die angegebene Datei Zeile für Zeile und leitet Informationen an nachgelagerte Methoden weiter
        /// </summary>
        /// <param name="vm"></param>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        public static void ReadDataBlockFile(MainWindowViewModel vm, string source, string destination)
        {
            _lastByte = 0;
            _actByte = 0;
            _lastBit = 0;
            _actBit = 0;
            _lastType = "";
            _type = "";
            _nextType = "";
            _dbname = vm.tbDBName;

            string xmlfile = "";
            string bmk = "";
            string actualV = "";
            string group = vm.tbBezeichnung;
            const string sep = ".";
            int level = -1;
            int id = 1;
            var nameTree = new string[10];

            // Datei einlesen
            var list = File.ReadAllLines(source).ToList();
            int lineCounter = 0;

            // Die zu untersuchenden Zeilen zaehlen
            foreach (string item in list)
            {
                if (item.Contains("END_DATA_BLOCK") || item.Contains("BEGIN"))
                {
                    // Ende erreicht
                    break;
                }
                lineCounter++;
            }

            // in Array fuer Auswertung
            var lines = list.ToArray();
            bool dbstart = false;
            bool read = true;
            vm.pbMaximum = lineCounter;

            int i = 0;
            while (read)
            {
                Console.WriteLine(lines[i]);

                string node;
                string propName = "";

                // Jede Zeile nach ihrer Art analysieren
                if (lines[i].ToUpper().Contains("DATA_BLOCK"))
                    propName = "DATA_BLOCK";
                if (lines[i].ToUpper().Trim() == "VAR")
                    propName = "VAR";
                if (lines[i].ToUpper().Trim() == "STRUCT")
                    propName = "STRUCT";
                if (lines[i].ToUpper().Contains(": STRUCT"))
                    propName = ": STRUCT";
                if (lines[i].ToUpper().Contains("END_STRUCT"))
                    propName = "END_STRUCT";
                if (lines[i].ToUpper().Trim() == "END_VAR")
                    propName = "END_VAR";
                if (lines[i].ToUpper().Contains(": ARRAY"))
                    propName = "ARRAY";
                if (!lines[i].ToUpper().Contains("END_STRUCT") && !lines[i].ToUpper().Contains("STRUCT") &&
                    !lines[i].ToUpper().Contains(": ARRAY") && !lines[i].ToUpper().Contains("VAR") &&
                    dbstart)
                    propName = "Node";
                if (i == lineCounter - 1)
                {
                    read = false;
                    propName = "END_DATA_BLOCK";
                }


                string defaultV;
                string comment;
                string tempName;
                int[] array;
                switch (propName)
                {
                    case "DATA_BLOCK":
                        xmlfile = CreateDataBlockXml(destination, lines[i]);
                        CreateXmlStart(destination, xmlfile);
                        break;
                    case "VAR":
                        level++; // neue Ebene
                        dbstart = true;
                        break;
                    case "STRUCT":
                        level++; // neue Ebene
                        dbstart = true;
                        break;
                    case ": STRUCT":
                        level++; // neue Ebene
                        int nameSep = lines[i].IndexOf(":", StringComparison.Ordinal); // Anzahl Zeichen ohne : Struct

                        nameTree[level] =
                            sep + lines[i].Substring(0, nameSep).Trim(); // Namen auslesen und Punkt davor setzen

                        break;
                    case "END_STRUCT":
                        // Namensbaum zurueck setzen
                        nameTree[level + 1] = "";
                        nameTree[level] = "";
                        level--;            // Ebene entfernen
                        _byteInStruct = 0;  // ist das Sturkturende erreicht, wird der Zaehler auf 0 gesetzt

                        if (level == -1)
                        {
                            // Ende des Bausteins erreicht
                            dbstart = false;
                        }
                        break;
                    case "END_VAR":
                        level--;            // Ebene entfernen
                        dbstart = false;
                        break;
                    case "ARRAY":

                        // Start des Arrays bei?
                        int startLength = int.Parse(lines[i].Split('[')[1].Split('.')[0].Trim());
                        // Ende des Arrays bei?
                        int length = int.Parse(lines[i].Split('.')[2].Split(']')[0].Trim());
                        for (int j = 0; j <= length - startLength; j++)
                        {
                            // Namen auslesen
                            node = lines[i].Split(':')[0].Trim() + "_" + j;
                            defaultV = "";

                            // Typ auslesen
                            try
                            {
                                _type = lines[i].Split('f')[1].Split(';')[0].Trim(); // f von of
                            }
                            catch (Exception exception)
                            {
                                Console.WriteLine(exception.Message);
                                Console.WriteLine(@"Kein Typ gefunden");

                                _type = "";
                            }
                            // Kommentar
                            comment = "Reserve";

                            // Elementnamen und Strukturnamen zusammenhängen
                            tempName = "";
                            nameTree[9] = sep + node;
                            for (int y = 0; y < nameTree.Length; y++)
                                tempName += nameTree[y];

                            // Adresse holen
                            array = GetStartAddress(_type);

                            tempName = tempName.Substring(1, tempName.Length - 1).Trim();
                            // XmlElement schreiben
                            CreateXmlElement(destination, xmlfile, '"' + id.ToString() + '"', '"' + group + '"',
                                tempName, array[0].ToString(), array[1].ToString(), _type, defaultV, actualV, bmk,
                                comment);

                            id++;
                        }

                        break;
                    case "Node":

                        // Elementnamen auslesen
                        node = lines[i].Split(':')[0].Trim();

                        if (level > -1)
                        {
                            // Typ auslesen
                            try
                            {
                                if (lines[i].ToUpper().Contains("REAL"))
                                    _type = "REAL";
                                else if (lines[i].ToUpper().Contains("BOOL"))
                                    _type = "BOOL";
                                else if (lines[i].ToUpper().Contains("INT"))
                                    _type = "INT";
                                else if (lines[i].ToUpper().Contains("DINT"))
                                    _type = "DINT";
                                else if (lines[i].ToUpper().Contains("WORD"))
                                    _type = "WORD";
                                else if (lines[i].ToUpper().Contains("DWORD"))
                                    _type = "DWORD";
                                else if (lines[i].ToUpper().Contains("BYTE"))
                                    _type = "BYTE";
                            }
                            catch (Exception exception)
                            {
                                Console.WriteLine(exception.Message);
                                Console.WriteLine(@"Keinen Typ gefunden");
                                _type = "";
                            }

                            // Naechsten Typ auslesen -> wichtig, wenn der aktuelle Typ BOOL ist!
                            try
                            {
                                _endStruct = lines[i + 1].ToUpper().Contains("END_STRUCT");
                                _nextType = lines[i + 1].Split(':')[1].Split(';')[0].Trim().ToUpper();
                            }
                            catch (Exception exception)
                            {
                                Console.WriteLine(exception.Message);
                                _nextType = "";
                            }

                            // Kommentar auslesen
                            try
                            {
                                comment = lines[i].Split(new[] { "//" }, StringSplitOptions.None)[1].Trim();
                            }
                            catch (Exception exception)
                            {
                                Console.WriteLine(exception.Message);
                                Console.WriteLine(@"Keinen Kommentar gefunden");
                                comment = "";
                            }

                            // Wurde ein Ort angegeben?
                            try
                            {
                                bmk = "=" + comment.Split('=')[1].Split(' ')[0].Trim();
                            }
                            catch (Exception exception)
                            {
                                Console.WriteLine(exception.Message);
                                Console.WriteLine(@"Keine Location gefunden");
                                bmk = "";
                            }

                            if (bmk != "")
                            {
                                string temp = comment;
                                comment = temp.Substring(bmk.Length).Trim();
                            }

                            // Elementnamen und Strukturnamen zusammenhängen
                            tempName = "";
                            nameTree[9] = sep + node;
                            for (int j = 0; j < nameTree.Length; j++)
                                tempName += nameTree[j];

                            defaultV = "";
                            tempName = tempName.Substring(1, tempName.Length - 1).Trim();

                            // in jeder Zeile nach dem zusammengehaengten Namen suchen, um Default-Werte auszulesen
                            foreach (string line in lines)
                            {
                                if (line.Split(':')[0].Trim() == tempName)
                                {
                                    string tempValue;
                                    // Anfangswert auslesen
                                    try
                                    {
                                        tempValue = line.Split(':')[1].Split(';')[0].Split('=')[1].Trim();
                                    }
                                    catch (Exception exception)
                                    {
                                        Console.WriteLine(exception.Message);
                                        tempValue = "";
                                    }
                                    try
                                    {
                                        defaultV = float.Parse(tempValue, NumberStyles.Float,
                                                CultureInfo.InvariantCulture)
                                            .ToString(CultureInfo.InvariantCulture);
                                    }
                                    catch (Exception exception)
                                    {
                                        Console.WriteLine(exception.Message);
                                        Console.WriteLine(@"Keinen Default-Wert gefunden");
                                        defaultV = tempValue;
                                    }
                                    break;
                                }
                            }

                            // Adresse holen
                            array = GetStartAddress(_type);

                            // XmlElement schreiben
                            CreateXmlElement(destination, xmlfile, '"' + id.ToString() + '"', '"' + group + '"',tempName,
                                array[0].ToString(), array[1].ToString(), _type, defaultV, actualV, bmk, comment);

                            id++; // id hochzaehlen
                        }
                        break;
                    case "END_DATA_BLOCK":

                        // Ende der Datei erreicht und XML abschliessen
                        CreateXmlEnd(destination, xmlfile);

                        vm.pbValue = lines.Length;
                        vm.tblProgress = "Konvertierung abgeschlossen";
                        return;
                }
                i++;
                vm.pbValue = i + 1;
                vm.tbAusgabe += i + 1 + ": " + lines[i] + Environment.NewLine;
                Console.ReadLine();
            }
        }

        /// <summary>
        ///     Berechnet die Start- und Bitadresse je nach Datentyp
        /// </summary>
        /// <param name="type">aktueller Datentyp</param>
        /// <returns></returns>
        public static int[] GetStartAddress(string type)
        {
            var returnValue = new int[2];
            int @byte = _lastByte;
            int bit = _lastBit;

            switch (type)
            {
                case "REAL":
                    @byte += 4;
                    bit = 0;
                    break;
                case "BYTE":
                    @byte += 1;
                    bit = 0;
                    break;
                case "INT":
                    @byte += 2;
                    bit = 0;
                    break;
                case "DINT":
                    @byte += 4;
                    bit = 0;
                    break;
                case "WORD":
                    @byte += 2;
                    bit = 0;
                    break;
                case "DWORD":
                    @byte += 4;
                    bit = 0;
                    break;
                case "BOOL":

                // Start eines neuen Bytes
                    if (bit == 0)
                    {
                    // Byte in der Struktur hochzaehlen 
                    // wird bei Erreichen von END_STRUCT auf 0 gesetzt s.o. !
                        _byteInStruct++;
                    }

                    // nur einzelne Bits hochzaehlen, wenn der naechste Typ (oder der letzte Typ ??) bool ist
                    // und die naechste Zeile nicht das Strukturende ist oder wenn das Byte vollstaendig gefuellt ist
                    if ((_nextType == "BOOL" || _lastType == "BOOL") && (!_endStruct)) // || _lastBit == 7))
                    { 
                        // ist das Byte noch nicht voll -> Bit hochzaehlen
                        if (_lastBit < 7)
                        {
                            @byte += 0;
                            bit += 1;
                        }
                        // Byte ist voll -> Byte hochzaehlen
                        else
                        {
                            @byte += 1;
                            bit = 0;
                        }
                    }
                    // Byte um 2 erhoehen, wenn die Struktur eine ungerade anzahl von Bytes hat
                    else if (_byteInStruct % 2 != 0) // Modulo
                    {
                        @byte += 2;
                        bit = 0;
                        _endStruct = false;
                    }
                    // Andernfalls Byte um 1 erhöhen
                    else
                    {
                        @byte += 1;
                        bit = 0;
                    }

                break;
                default:
                    throw new Exception();
            }


            if (_lastType != type && @byte != _lastByte)
            {
            }

            _lastType = type;

            _actByte = _lastByte;
            _lastByte = @byte;

            _actBit = _lastBit;
            _lastBit = bit;

            returnValue[0] = _actByte;
            returnValue[1] = _actBit;

            return returnValue;
        }


        /// <summary>
        ///     Legt die XML-Datei an
        /// </summary>
        /// <param name="path"></param>
        /// <param name="line"></param>
        /// <returns></returns>
        public static string CreateDataBlockXml(string path, string line)
        {
            string extension = ".xml";

            if (_dbname == "")
                _dbname = "DBxx";

            try
            {
                FileStream fileStream = File.Create(path + "\\" + _dbname + extension);
                fileStream.Close();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }

            return _dbname + extension;
        }

        /// <summary>
        ///     Haengt den Start-Tag an die XML-Datei
        /// </summary>
        /// <param name="path"></param>
        /// <param name="file"></param>
        public static void CreateXmlStart(string path, string file)
        {
            using (StreamWriter sw = File.CreateText(path + "\\" + file))
            {
                string dbNumber = "";
                
                try
                {
                    dbNumber = _dbname.Split('B')[1];
                }
                catch (Exception)
                {
                    // Ignored
                }
                sw.WriteLine("<DataBlock DBNumber='" + dbNumber + "'>");

                sw.Close();
            }
        }

        /// <summary>
        ///     Hängt ein neues Element in die XML-Datei
        /// </summary>
        /// <param name="destination">Zielordner</param>
        /// <param name="file">Dateiname</param>
        /// <param name="id">Fortlaufende Nummer</param>
        /// <param name="group">Bezeichnung bzw. GroupName-Attribute des Elements</param>
        /// <param name="name">Name des Elements</param>
        /// <param name="start">Startadresse / Byte</param>
        /// <param name="bit">Bitadresse</param>
        /// <param name="type">Datentyp (z.B. REAL, BOOL, ..)</param>
        /// <param name="defaultV">Anfangswert aus der SPS</param>
        /// <param name="actualV">Istwert</param>
        /// <param name="location"></param>
        /// <param name="comment">Kommentar der Adresse aus der SPS</param>
        public static void CreateXmlElement(string destination, string file, string id, string group, string name,
            string start, string bit,
            string type, string defaultV, string actualV, string location, string comment)
        {
            using (StreamWriter sw = File.AppendText(destination + "\\" + file))
            {
                sw.WriteLine("<Element Id=" + id + " GroupName=" + group + ">");
                sw.WriteLine("\t<Name>" + name + "</Name>");
                sw.WriteLine("\t<StartAddress>" + start + "</StartAddress>");
                sw.WriteLine("\t<BitNumber>" + bit + "</BitNumber>");
                sw.WriteLine("\t<Type>" + type + "</Type>");
                if (defaultV == "")
                    defaultV = "0";
                sw.WriteLine("\t<Default>" + defaultV + "</Default>");
                if (actualV == "")
                    actualV = "0";
                sw.WriteLine("\t<Actual>" + actualV + "</Actual>");
                if ((group.Contains("ERROR") || group.Contains("WARNING") || group.Contains("INFO")) && location != "")
                    sw.WriteLine("\t<Location>" + location + "</Location>");
                if (comment == "")
                    comment = "not set";
                sw.WriteLine("\t<Comment>" + comment + "</Comment>");
                sw.WriteLine("</Element>");

                sw.Close();
            }
            Thread.Sleep(10);
        }

        /// <summary>
        ///     Haengt den End-Tag an die XML-Datei
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="file"></param>
        public static void CreateXmlEnd(string destination, string file)
        {
            using (StreamWriter sw = File.AppendText(destination + "\\" + file))
            {
                sw.WriteLine("</DataBlock>");
                sw.Close();
            }
        }
    }
}