using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.XPath;
using Sgml;

public class Driver
{
	const string android_ns = "http://schemas.android.com/apk/res/android";
	
	public static void Main ()
	{
		SaveAsXml ("http://developer.android.com/reference/android/R.styleable.html", "R.styleable.xml");
		//SaveAsXml ("http://developer.android.com/reference/android/view/View.html", "View.xml");

		var doc = new XmlDocument ();

		doc.Load ("R.styleable.xml");
		var content = doc.SelectSingleNode ("/html/body//div[@id='jd-content']");
		var consts = content.SelectSingleNode ("//table[@id='constants']");
		var atts = new List<string> ();
		var sw = new StringWriter ();
		sw.WriteLine ("<android-attribute-defs xmlns:android='http://schemas.android.com/apk/res/android'>");
		foreach (XmlNode tr in consts.SelectNodes ("tr")) {
			var td0 = tr.SelectSingleNode ("td[1]");
			if (td0 == null || td0.InnerText.Trim () != "int[]")
				continue;
			XmlNode xn = tr.SelectSingleNode ("td[@class='jd-linkcol']/a/text()");
			var name = xn.Value.Trim ();

			var n = content.SelectSingleNode ("A[@NAME='" + name + "']");
			sw.WriteLine ("<class name='" + name + "'>");
			n = NextElement (n);
			if (((XmlElement) n).GetAttribute ("class") != "jd-details api apilevel-")
				throw new Exception ("huh? " + n.LocalName);

			n = n.SelectSingleNode ("div[@class='jd-details-descr']/div//table"); // -> table of Attribute/Description
			if (n == null)
				Console.Error.WriteLine ("Empty table: " + name);
			else {
				var nodes = n.SelectNodes ("tr/td[1]/code/code/a");
				foreach (XmlNode attr in nodes) {
					var a = attr.InnerText;
					if (!atts.Contains (a))
						atts.Add (a);
					sw.WriteLine ("<a>" + a + "</a>");
				}
			}
			sw.WriteLine ("</class>");
		}
		sw.WriteLine ("</android-attribute-defs>");

		using (var tw = File.CreateText ("all-known-attributes.xml"))
			tw.Write (sw.ToString ());

		using (var tw = File.CreateText ("schemas.android.com.apk.res.android.xsd")) {
			tw.WriteLine (@"
<xs:schema
  xmlns:xs='http://www.w3.org/2001/XMLSchema'
  targetNamespace='" + android_ns + @"'
  xmlns:android='" + android_ns + @"'>
");
			foreach (var a in atts)
				tw.WriteLine ("<xs:attribute name='{0}' type='xs:string' />", a.Substring (a.IndexOf (':') + 1));
			tw.WriteLine ("</xs:schema>");
		}
	}
	
	static XmlNode NextElement (XmlNode n)
	{
		do {
			n = n.NextSibling;
		} while (n != null && n.NodeType != XmlNodeType.Element);
		return n;
	}
	
	static void SaveAsXml (string url, string fileToSave)
	{
		if (File.Exists (fileToSave))
			return;

		var stream = new XmlUrlResolver ().GetEntity (new Uri (url), null, typeof (Stream)) as Stream;
		var xr = new SgmlReader () { InputStream = new StreamReader (stream) };
		var xw = XmlWriter.Create (fileToSave);
		xr.MoveToContent ();
		do {
			xw.WriteNode (xr, false);
			xw.Flush ();
		} while (xr.Read ());
		xw.Close ();
	}
}

