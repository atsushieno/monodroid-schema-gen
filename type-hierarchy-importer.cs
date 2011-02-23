using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Xml;
using Sgml;

public class Driver
{
	public static void Main (string [] args)
	{
		new Driver ().Run (args);
	}

	const string android_ns = "http://schemas.android.com/apk/res/android";

	List<string> atts = new List<string> ();
	TextWriter fs1;

	public void Run (string [] args)
	{
		fs1 = File.CreateText ("type-hierarchy.xml");
		fs1.WriteLine ("<android-hierarchy xmlns:android='" + android_ns + "'>");

		foreach (var arg in new string [] {"http://developer.android.com/reference/android/view/View.html"}) {
			var baseUrl = resolver.ResolveUri (null, arg);
			var doc = FetchXmlDocument (baseUrl);

			ProcessDocument (baseUrl);

			foreach (XmlElement node in doc.SelectNodes ("//div[@id='subclasses-direct' or @id='subclasses-indirect']")) {
				foreach (XmlElement link in node.SelectNodes ("div/table[@class='jd-sumtable-expando']/tr/td[@class='jd-linkcol']/a[@href]")) {
					if (link.PreviousSibling != null && link.PreviousSibling.Value.Contains ("extends"))
						continue; // it is a link to generic type argument.
					var durl = resolver.ResolveUri (baseUrl, link.GetAttribute ("href"));
					ProcessDocument (durl);
				}
			}
		}
		fs1.WriteLine ("</android-hierarchy>");
		fs1.Close ();
	}

	string GetName (Uri baseUrl)
	{
		var url = baseUrl.ToString ();
		url = url.Substring (0, url.Length - ".html".Length);
		return url.Substring (url.LastIndexOf ('/') + 1);
	}

	XmlResolver resolver = new XmlUrlResolver ();

	StreamReader FetchWebText (Uri url)
	{
		var wc = new WebClient ();
		return new StreamReader (new XmlUrlResolver ().GetEntity (url, null, typeof (Stream)) as Stream);
	}

	XmlDocument FetchXmlDocument (Uri url)
	{
		var sr = FetchWebText (url);
		var xr = new SgmlReader () { InputStream = sr };
		var doc = new XmlDocument ();
		doc.Load (xr);
		sr.Close ();
		xr.Close ();
		return doc;
	}

	void ProcessDocument (Uri url)
	{
		Console.Error.WriteLine ("Processing {0}...", url);

		var doc = FetchXmlDocument (url);

		var baseTable = doc.SelectSingleNode ("//table[@class='jd-inheritance-table']");
		var baseTypeName = baseTable.SelectSingleNode ("tr[last() - 1]/td[last()]").InnerText;

		fs1.WriteLine ("<class name='{0}' url='{1}' base='{2}'>", GetName (url), url, baseTypeName);
/*
		var table = doc.SelectSingleNode ("//table[@id='lattrs']");
		if (table != null) {
			var nodes = table.SelectNodes ("tr[contains(@class,'api')]");
			foreach (XmlNode node in nodes) {
				var attr = node.SelectSingleNode ("td[1]//text()");
				var method = node.SelectSingleNode ("td[2]//text()");
				var a = attr.InnerText;
				fs1.WriteLine ("<a>{0}</a>", a);//node.SelectSingleNode ("td[1]"));
				if (!atts.Contains (a))
					atts.Add (a);
			}
		}
*/
		fs1.WriteLine ("</class>");
		fs1.Flush ();
	}
}

