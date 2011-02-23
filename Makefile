SOURCES1 = type-hierarchy-importer.cs extlibs/SgmlReader/sgmlreaderdll/*.cs
SOURCES2 = get-android-r-styleable.cs extlibs/SgmlReader/sgmlreaderdll/*.cs
SOURCES3 = monodroid-schema-gen.cs
SOURCES4 = convert-csv.cs
XML_RESOURCES = type-hierarchy.xml all-known-attributes.xml
XML_RESOURCES2 = layout_schema_enumerations.xml

all: android-layout-xml.xsd schemas.android.com.apk.res.android.xsd

android-layout-xml.xsd : monodroid-schema-gen.exe $(XML_RESOURCES)
	mono --debug ./monodroid-schema-gen.exe "$(MONO_ANDROID_DLL)"

type-hierarchy-importer.exe : $(SOURCES1)
	dmcs -debug $(SOURCES1)

get-android-r-styleable.exe : $(SOURCES2)
	dmcs -debug $(SOURCES2)

monodroid-schema-gen.exe : $(SOURCES3)
	dmcs -debug $(SOURCES3)

convert-csv.exe : $(SOURCES4)
	dmcs -debug $(SOURCES4)

type-hierarchy.xml : type-hierarchy-importer.exe 
	mono --debug type-hierarchy-importer.exe

schemas.android.com.apk.res.android.xsd all-known-attributes.xml : get-android-r-styleable.exe $(XML_RESOURCES2)
	mono --debug get-android-r-styleable.exe

layout_schema_enumerations.xml : convert-csv.exe
	mono --debug convert-csv.exe > layout_schema_enumerations.xml

clean:
	rm -f type-hierarchy-importer.exe type-hierarchy-importer.exe.mdb \
		get-android-r-styleable.exe get-android-r-styleable.exe.mdb \
		convert-csv.exe convert-csv.exe.mdb \
		monodroid-schema-gen.exe monodroid-schema-gen.exe.mdb \
		$(XML_RESOURCES)  $(XML_RESOURCES2) android-layout-xml.xsd
