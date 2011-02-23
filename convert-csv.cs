using System;
using System.IO;
using System.Linq;
using System.Xml;

public class CsvConvert
{
	public static void Main ()
	{
		Console.WriteLine ("<enumerated-values>");
		foreach (var l in File.ReadAllText("layout_schema_enumerations.csv").Replace ("\r\n", "\n").Split ('\n')) {
			if (l.Length == 0) continue;
			var ad = l.Split (',');
			var name = ad [0];
			string opt = null;
			switch (name [name.Length - 1]) {
			case '*':
				opt = " else=\"allowed\"";
				goto case ' ';
			case '^':
				opt = " flags=\"true\"";
				goto case ' ';
			case ' ':
				name = name.Substring (0, name.Length - 1);
				break;
			}
			Console.WriteLine ("  <attr name=\"{0}\"{1}>", name, opt);
			foreach (var item in ad.Skip (1))
				if (item.Length > 0)
					Console.WriteLine ("    <value>{0}</value>", item);
			Console.WriteLine ("  </attr>");
		}
		Console.WriteLine ("</enumerated-values>");
	}
}

