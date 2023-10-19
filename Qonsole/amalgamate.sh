rm /tmp/_hd
rm /tmp/_dm
rm /tmp/_ft
rm /tmp/QonsoleAmalgamated.cs

printf "// THIS FILE WAS GENERATED\n\n \
#if UNITY_STANDALONE || UNITY_2021_0_OR_NEWER\n \
#define HAS_UNITY\n \
#endif\n \
\n \
#define QONSOLE_BOOTSTRAP // if this is defined, the console will try to bootstrap itself \n \
#define QONSOLE_BOOTSTRAP_EDITOR // if this is defined, the console will try to bootstrap itself in the editor \n \
\n \
namespace QON {\n\n\n \
" > /tmp/_hd

printf "\n\n} namespace QON {\n\n\n" > /tmp/_dm
printf "}\n" > /tmp/_ft

cp Qonsole.cs        /tmp/__1.cs && sed -i 's/#define/\/\/#define/' /tmp/__1.cs
cp ../Cellophane.cs  /tmp/__2.cs && sed -i 's/#define/\/\/#define/' /tmp/__2.cs
cp ../CodePage437.cs /tmp/__3.cs && sed -i 's/#define/\/\/#define/' /tmp/__3.cs
cp ../QGL.cs         /tmp/__4.cs && sed -i 's/#define/\/\/#define/' /tmp/__4.cs
cp ../QUI.cs         /tmp/__5.cs && sed -i 's/#define/\/\/#define/' /tmp/__5.cs
cp ../AppleFont.cs   /tmp/__6.cs && sed -i 's/#define/\/\/#define/' /tmp/__6.cs
cp ../KeyBinds.cs    /tmp/__7.cs && sed -i 's/#define/\/\/#define/' /tmp/__7.cs
cp ../Qonche.cs      /tmp/__8.cs && sed -i 's/#define/\/\/#define/' /tmp/__8.cs
cp ../NokiaFont.cs   /tmp/__9.cs && sed -i 's/#define/\/\/#define/' /tmp/__9.cs
 
cat \
    /tmp/_hd \
    /tmp/__1.cs /tmp/_dm \
    /tmp/__2.cs /tmp/_dm \
    /tmp/__3.cs /tmp/_dm \
    /tmp/__4.cs /tmp/_dm \
    /tmp/__5.cs /tmp/_dm \
    /tmp/__6.cs /tmp/_dm \
    /tmp/__7.cs /tmp/_dm \
    /tmp/__8.cs /tmp/_dm \
    /tmp/__9.cs /tmp/_dm \
    /tmp/_ft \
> /tmp/QonsoleAmalgamated.cs
