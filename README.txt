-------------------S7SourceToXmlParser-----------------------

Mit diesem Werkzeug kann eine S7 DB-Quelle in eine XML-Datei konvertiert werden,

um sie dann in ein .Net-Projekt einzulesen und um auf einzelene Adressen zuzugreifen.


Die Quelldatei ist die Quelle eines Datenbausteins der SPS.

Mehrere Bausteine in einer Datei werden nicht unterstuezt.


Es empfiehlt sich die Quelle etwas aufzubereiten und z.B. unnoetige Kommentare zu entfernen.





Die Xml-Datei ist wie folgt aufgebaut:

<DataBlock>
     <Element Id="1" GroupName="INFO">
	   <Name>mess_0001</Name>
	   <StartAdress>0</StartAdress>
	   <BitNumber>0</BitNumber>
	   <Type>BOOL</Type>
	   <Comment>Kommentar 1</Comment>
     </Element>
     <Element Id="2" GroupName="Info">
	   <Name>mess_0002</Name>
	   <StartAdress>0</StartAdress>
	   <BitNumber>1</BitNumber>
	   <Type>BOOL</Type>
	   <Comment>Kommentar 2</Comment>
     </Element>
</DataBlock>
