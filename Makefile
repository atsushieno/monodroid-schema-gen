SOURCES1 = type-hierarchy-importer.cs extlibs/SgmlReader/sgmlreaderdll/*.cs
SOURCES2 = get-android-r-styleable.cs extlibs/SgmlReader/sgmlreaderdll/*.cs
SOURCES3 = monodroid-schema-gen.cs
XML_RESOURCES = type-hierarchy.xml all-known-attributes.xml schemas.android.com.apk.res.android.xsd

all: android-layout-xml.xsd

android-layout-xml.xsd : monodroid-schema-gen.exe $(XML_RESOURCES)
	mono --debug ./monodroid-schema-gen.exe "$(MONO_ANDROID_DLL)"

type-hierarchy-importer.exe : $(SOURCES1)
	dmcs -debug $(SOURCES1)

get-android-r-styleable.exe : $(SOURCES2)
	dmcs -debug $(SOURCES2)

monodroid-schema-gen.exe : $(SOURCES3)
	dmcs -debug $(SOURCES3)

type-hierarchy.xml : type-hierarchy-importer.exe 
	mono --debug type-hierarchy-importer.exe

all-known-attributes.xml : get-android-r-styleable.exe
	mono --debug get-android-r-styleable.exe

schemas.android.com.apk.res.android.xsd : monodroid-schema-gen.exe $(XML_RESOURCES)
	mono --debug monodroid-schema-gen.exe

clean:
	rm -f type-hierarchy-importer.exe type-hierarchy-importer.exe.mdb \
		get-android-r-styleable.exe get-android-r-styleable.exe.mdb \
		monodroid-schema-gen.exe monodroid-schema-gen.exe.mdb \
		$(XML_RESOURCES) android-layout-xml.xsd
