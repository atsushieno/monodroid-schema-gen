using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Schema;

namespace Commons.AndroidSchemaGen
{
	public class Driver
	{
		const string android_ns = "http://schemas.android.com/apk/res/android";

		public static void Main (string [] args)
		{
			new Driver ().Run (args);
		}
		
		List<Assembly> asses = new List<Assembly> ();
		List<Type> views = new List<Type> ();
		Type view;
		XmlDocument doc = new XmlDocument ();
		List<string> java_views = new List<string> ();
		Dictionary<string,string> java_base_types = new Dictionary<string,string> ();
		Dictionary<string,List<string>> attributes;

		// look for Android.Views.View
		public void Run (string [] args)
		{
			LoadXml ();

			foreach (string arg in args)
				asses.Add (Assembly.ReflectionOnlyLoadFrom (arg));

			foreach (var ass_ in asses)
				foreach (var type in ass_.GetTypes ())
					if (type.Name == "View" && type.FullName == "Android.Views.View") {
						view = type;
						break;
					}

			foreach (var ass_ in asses)
				foreach (var type in ass_.GetTypes ())
					if (IsView (type))
						views.Add (type);

			var xs = new XmlSchema ();
			xs.Namespaces.Add ("xs", XmlSchema.Namespace);
			xs.Namespaces.Add ("android", android_ns);
			var xsimp = new XmlSchemaImport () {SchemaLocation = "schemas.android.com.apk.res.android.xsd", Namespace = android_ns};
			xs.Includes.Add (xsimp);
			var choice = new XmlSchemaChoice ();
			var xsg = new XmlSchemaGroup () { Name = "any-view" };
			xsg.Particle = choice;
			xs.Items.Add (xsg);

			var xseView = new XmlSchemaElement () { Name = "View", SchemaTypeName = new XmlQualifiedName ("View") };
			xs.Items.Add (xseView);
			var xsctView = new XmlSchemaComplexType () { Name = "View" };
			xs.Items.Add (xsctView);

			foreach (var att in CreateAttributes ("View"))
				xsctView.Attributes.Add (att);

			foreach (var type in views) {
				if (type == view) // we define individually.
					continue;

				var nameString = StripGenericName (type);
				if (!java_views.Contains (nameString)) {
					Console.Error.WriteLine ("Skipped " + nameString);
					continue;
				}
				// FIXME: should not be special case.
				if (type.IsGenericType && nameString == "AdapterView")
					continue;

				// <xs:element name="FooBarView">
				//  <xs:complexType>
				//   <xs:complexContent>
				//    <xs:extension base="FooBarBaseView">
				//     <xs:group ref="any-view" minOccurs=0 maxOccurs="unbounded"> <!-- if it is ViewGroup (and can have children) -->
				//    </xs:extension>
				var xse = new XmlSchemaElement () { Name = nameString, SchemaTypeName = new XmlQualifiedName (nameString) };
				AddAnnotation (xse, "Runtime Type: " + type.FullName);
				xs.Items.Add (xse);
				var xsct = new XmlSchemaComplexType () { Name = nameString };
				xs.Items.Add (xsct);
				var xscm = new XmlSchemaComplexContent ();
				xsct.ContentModel = xscm;
				var xsce = new XmlSchemaComplexContentExtension () { BaseTypeName = new XmlQualifiedName (StripGenericName (type.BaseType)) };
				xscm.Content = xsce;
				
				// expand attributes
				foreach (var att in CreateAttributes (xse.Name))
					xsce.Attributes.Add (att);
				
				// add ref-to-this-element element to "any-view" group which could be used for children's applicable group.
				choice.Items.Add (new XmlSchemaElement () { RefName = new XmlQualifiedName (nameString) });
				
				if (IsDirectViewManagerImplementor (type) && !IsDirectViewManagerImplementor (type.BaseType))
					xsce.Particle = new XmlSchemaGroupRef () { RefName = new XmlQualifiedName ("any-view"), MinOccurs = 0, MaxOccursString = "unbounded" };
			}
			using (var xw = XmlWriter.Create ("android-layout-xml.xsd", new XmlWriterSettings { Indent = true }))
				xs.Write (xw);
		}
		
		IEnumerable<XmlSchemaAttribute> CreateAttributes (string elem)
		{
			var lp = attributes.FirstOrDefault (p => p.Key == elem);
			if (lp.Value != null)
				foreach (var attr in lp.Value)
					yield return new XmlSchemaAttribute () { RefName = new XmlQualifiedName (attr, android_ns) };
		}
		
		string StripGenericName (Type type)
		{
			return type.IsGenericType ? type.Name.Substring (0, type.Name.IndexOf ('`')) : type.Name;
		}
		
		void AddAnnotation (XmlSchemaAnnotated a, string text)
		{
			a.Annotation = a.Annotation ?? new XmlSchemaAnnotation ();
			AddAnnotation (a.Annotation, text);
		}
		
		void AddAnnotation (XmlSchemaAnnotation a, string text)
		{
			a.Items.Add (new XmlSchemaDocumentation () { Markup = new XmlNode [] {doc.CreateTextNode (text) } });
		}
		
		bool IsDirectViewManagerImplementor (Type type)
		{
			return IsViewManager (type) && !IsViewManager (type.BaseType);
		}
		
		bool IsViewManager (Type type)
		{
			return type.GetInterfaces ().Any (t => t.FullName == "Android.Views.IViewManager");
		}
		
		bool IsView (Type type)
		{
			if (type == null)
				return false;
			if (type.BaseType == view)
				return true;
			if (views.Contains (type.BaseType))
				return true;
			if (!asses.Contains (type.Assembly))
				return false;
			return IsView (type.BaseType);
		}

		void LoadXml ()
		{
			var dic = new Dictionary<string,List<string>> ();
			attributes = dic;
			
			doc.Load ("all-known-attributes.xml");
			
			foreach (XmlElement cls in doc.SelectNodes ("/android-attribute-defs/class")) {
				var l = new List<string> ();
				string name = cls.GetAttribute ("name");

				// Attributes in nested type X_Y (e.g. LinearLayout_Layout) will be moved to X type.
				if (name.IndexOf ('_') > 0) {
					string subst = "View";//name.Substring (0, name.IndexOf ('_'));
					Console.Error.WriteLine ("Merging {0} into {1}", name, subst);
					name = subst;
				}
				if (dic.ContainsKey (name))
					l = dic [name];
				else
					dic [name] = l;

				foreach (XmlElement a in cls.SelectNodes ("a")) {
					var an = a.InnerText;
					string av = an.Substring (an.IndexOf (':') + 1);
					if (!l.Contains (av))
						l.Add (av);
				}
			}
			
			doc.Load ("type-hierarchy.xml");
			foreach (XmlElement cls in doc.SelectNodes ("/android-hierarchy/class")) {
				string name = cls.GetAttribute ("name");
				string bs = cls.GetAttribute ("base");
				bs = bs.Substring (bs.LastIndexOf ('.') + 1);
				java_views.Add (name);
				java_base_types.Add (name, bs);
			}
		}
	}
}
