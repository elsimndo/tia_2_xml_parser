-------------------S7SourceToXmlParser-----------------------

This tool can be used to convert an S7 DB source into an XML file,

in order to read it into a .Net project and to access individual addresses.


The source file is the source of a data block of the PLC.

Multiple blocks in one file are not supported.


It is advisable to edit the source a little and remove unnecessary comments, for example.




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
